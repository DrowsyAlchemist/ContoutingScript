using Contouring.Extentions;
using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace Contouring.Tools
{
    public class TargetStructuresCreator
    {
        private const uint MarginUpperBoundInMM = 50;
        private readonly CroppersFactory _croppersFactory;
        private uint? _marginFromCtv;

        private StructureSet StructureSet => Program.StructureSet;

        public TargetStructuresCreator(CroppersFactory croppersFactory)
        {
            _croppersFactory = croppersFactory;
        }

        public void Create()
        {
            Logger.WriteInfo("\tTargetStructuresCreator: Create");
            try
            {
                CreatePtvs();
            }
            catch (Exception error)
            {
                Logger.WriteError($"Error during creating ptv:\n\t{error}\n");
            }
            CreateOpt(from: StructureNames.PtvAll);
            CreateOpt(from: StructureNames.PtvT);
        }

        public uint SetMargin()
        {
            uint marginInMM = 0;
            bool isCorrectMargin = false;

            while (isCorrectMargin == false)
            {
                Console.Write("Ptv margin in mm: ");
                isCorrectMargin = uint.TryParse(Console.ReadLine(), out marginInMM);

                if (marginInMM <= 0)
                {
                    Logger.WriteError("Margin should be positive integer.");
                    isCorrectMargin = false;
                }
                if (marginInMM > MarginUpperBoundInMM)
                {
                    Logger.WriteError("Margin should be less than 50 mm.");
                    isCorrectMargin = false;
                }
            }
            _marginFromCtv = marginInMM;
            return marginInMM;
        }

        public void CreatePtvOptMinus()
        {
            Logger.WriteInfo("\tTargetStructuresCreator: CreatePtvOptMinus");

            try
            {
                Structure ptv;

                if (StructureSet.Contains(StructureNames.PtvOpt))
                    ptv = StructureSet.GetStructure(StructureNames.PtvOpt);
                else
                    ptv = StructureSet.GetStructure(StructureNames.PtvAll);

                StructuresCropper cropperByCtv = _croppersFactory.Create(StructureNames.CtvAll);
                Structure ptvOptMinus = StructureSet.GetOrCreateStructure(StructureNames.PtvOptMinus, dicomType: StructureNames.PtvDicomType);
                ptvOptMinus.SegmentVolume = ptv.Margin(-1 * Config.OptMinusInnerMargin);
                ptvOptMinus.SegmentVolume = cropperByCtv.Crop(ptvOptMinus, Config.OptMinusMarginFromCtv, removePartInside: true);
            }
            catch (Exception error)
            {
                Logger.WriteError($"Error during {StructureNames.PtvOptMinus} creation.\n{error}");
            }
        }

        private void CreatePtvs()
        {
            List<Structure> ctvs = GetCtvs();
            List<Structure> ptvs = new List<Structure>();
            uint margin = (uint)_marginFromCtv;

            foreach (Structure ctv in ctvs)
            {
                try
                {
                    ptvs.Add(CreatePtvFromCtv(ctv, margin));
                }
                catch (Exception error)
                {
                    Logger.WriteError($"Can not create ptv from \"{ctv.Id}\":\n\t{error}\n");
                }
            }
            Structure ptvAll = StructureSet.GetOrCreateStructure(StructureNames.PtvAll, dicomType: StructureNames.PtvDicomType);
            ptvAll.SegmentVolume = ptvAll.Merge(ptvs.ToList());

            if (StructureSet.GetStructure(StructureNames.PtvAll).IsEmpty)
                throw new Exception($"{StructureNames.PtvAll} is empty.");
        }

        private Structure CreatePtvFromCtv(Structure ctv, uint marginFromCtvInMM)
        {
            string ptvName = StructureNames.PtvDicomType + ctv.Id.Substring(3);
            Structure ptv = StructureSet.GetOrCreateStructure(ptvName, dicomType: StructureNames.PtvDicomType);
            ptv.SegmentVolume = ctv.Margin(marginFromCtvInMM);
            return ptv;
        }

        private void CreateOpt(string from)
        {
            try
            {
                Structure fromStructure = StructureSet.GetStructure(from);
                string optName = GetOptName(from);
                var optStructure = StructureSet.GetOrCreateStructure(optName, dicomType: StructureNames.PtvDicomType);
                var cropperByBody = _croppersFactory.Create(StructureNames.Body);
                optStructure.SegmentVolume = cropperByBody.Crop(fromStructure, Config.CtvIntoBodyMargin, removePartInside: false);

                if (IsOptVolumeValid(fromStructure, optStructure) == false)
                {
                    Logger.WriteInfo($"\"{optName}\" has been removed.");
                    StructureSet.RemoveStructure(optStructure);
                }
            }
            catch (Exception error)
            {
                Logger.WriteWarning($"Can not create ptvOpt from \"{from}\":\n{error}\n");
            }
        }

        private string GetOptName(string from)
        {
            if (from.Contains(StructureNames.PtvAll))
                return StructureNames.PtvOpt;
            else
                return from + StructureNames.OptPostfix;
        }

        private bool IsOptVolumeValid(Structure from, Structure opt)
        {
            double volumeDifferenceInPercents = (1 - opt.Volume / from.Volume) * 100;

            Logger.WriteWarning($"Created {opt.Id} ({opt.Volume:F1}) from {from.Id} ({from.Volume:F1}).\n" +
                $"Difference: {volumeDifferenceInPercents:F1} %. Threshold: {Config.PtvOptThresholdInPercents:F1} %.");

            return volumeDifferenceInPercents > Config.PtvOptThresholdInPercents;
        }

        private List<Structure> GetCtvs()
        {
            List<Structure> ctvs = GetFilledCTVs();

            if (CtvIsHighResolution(ctvs))
                throw new Exception("Error: CTV is high resolution.");

            Structure ctvAll = StructureSet.GetOrCreateStructure(StructureNames.CtvAll, dicomType: StructureNames.CtvDicomType);
            ctvAll.SegmentVolume = ctvAll.Merge(ctvs);
            ctvs.Add(ctvAll);
            return ctvs;
        }

        private List<Structure> GetFilledCTVs()
        {
            try
            {
                return StructureSet.Structures
                    .Where(s => s.Id.ToLower().StartsWith(StructureNames.CtvDicomType.ToLower()))
                    .Where(s => s.IsEmpty == false)
                    .ToList();
            }
            catch (ArgumentNullException)
            {
                throw new Exception("Can not find filled ctv");
            }
        }

        private bool CtvIsHighResolution(List<Structure> ctvs)
        {
            foreach (Structure ctv in ctvs)
                if (ctv.IsHighResolution)
                    return true;

            return false;
        }
    }
}
