using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace ContouringScript
{
    public class TargetStructuresCreator
    {
        private const uint MarginUpperBoundInMM = 50;

        private Application Application => Program.Application;
        private StructureSet StructureSet => Program.StructureSet;

        public TargetStructuresCreator()
        {
        }

        public void Create()
        {
            try
            {
                CreatePtvs();
            }
            catch (Exception error)
            {
                Logger.WriteError($"Error during creating ptv:\n\t{error}\n");
            }
            Structure body = StructureSet.GetStructure(Config.BodyName);
            var cropperByBody = new StructuresCropper(body);
            CreateOpt(cropperByBody, from: Config.PtvAllName);
            CreateOpt(cropperByBody, from: Config.PtvTName);
        }

        public void CreatePtvOptMinus()
        {
            try
            {
                Structure ptvOpt = StructureSet.GetStructure(Config.PtvOptName);
                Structure ctv = StructureSet.GetStructure(Config.CtvAllName);
                StructuresCropper cropperByCtv = new StructuresCropper(ctv);

                Structure ptvOptMinus = StructureSet.GetOrCreateStructure(Config.PtvOptMinusName, dicomType: Config.PtvType);
                ptvOptMinus.SegmentVolume = ptvOpt.Margin(-1* Config.OptMinusInnerMargin);
                ptvOptMinus.SegmentVolume = cropperByCtv.Crop(ptvOptMinus, Config.OptMinusMarginFromCtv, removePartInside: true);
            }
            catch (Exception error)
            {
                Logger.WriteError($"Error during {Config.PtvOptMinusName} creation.\n{error}");
            }
        }

        private void CreatePtvs()
        {
            List<Structure> ctvs = GetCtvs();
            List<Structure> ptvs = new List<Structure>();
            uint marginFromCtvInMM = GetMargin();

            foreach (Structure ctv in ctvs)
            {
                try
                {
                    ptvs.Add(CreatePtvFromCtv(ctv, marginFromCtvInMM));
                }
                catch (Exception error)
                {
                    Logger.WriteError($"Can not create ptv from \"{ctv.Id}\":\n\t{error}\n");
                }
            }
            Structure ptvAll = StructureSet.GetOrCreateStructure(Config.PtvAllName, dicomType: Config.PtvType);
            MergeStructures(ptvAll, ptvs.ToList());

            if (StructureSet.GetStructure(Config.PtvAllName).IsEmpty)
                throw new Exception($"{Config.PtvAllName} is empty.");

            Application.SaveModifications();
        }

        private Structure CreatePtvFromCtv(Structure ctv, uint marginFromCtvInMM)
        {
            string ptvName = Config.PtvType + ctv.Id.Substring(3);
            Structure ptv = StructureSet.GetOrCreateStructure(ptvName, dicomType: Config.PtvType);
            ptv.SegmentVolume = ctv.Margin(marginFromCtvInMM);
            return ptv;
        }

        private uint GetMargin()
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
            return marginInMM;
        }

        private void CreateOpt(StructuresCropper cropperByBody, string from)
        {
            try
            {
                Structure fromStructure = StructureSet.GetStructure(from);
                string optName = GetOptName(from);
                var optStructure = StructureSet.GetOrCreateStructure(optName, dicomType: Config.PtvType);
                optStructure.SegmentVolume = cropperByBody.Crop(fromStructure, Config.TargetFromBody, removePartInside: false);

                if (IsOptVolumeValid(fromStructure, optStructure) == false)
                    StructureSet.RemoveStructure(optStructure);
            }
            catch (Exception error)
            {
                Logger.WriteWarning($"Can not create ptvOpt from \"{from}\":\n{error}\n");
            }
        }

        private string GetOptName(string from)
        {
            if (from.ToLower().Contains(Config.PtvAllName))
                return Config.PtvOptName;
            else
                return from + Config.OptPostfix;
        }

        private bool IsOptVolumeValid(Structure from, Structure opt)
        {
            double volumeDifferenceInPercents = (1 - opt.Volume / from.Volume) * 100;

            Logger.WriteWarning($"From {from.Id} ({from.Volume}) create {opt.Id} ({opt.Volume}). " +
                $"Difference: {volumeDifferenceInPercents}. Trashhold: {Config.PtvOptTrashholdInPercents}");

            return volumeDifferenceInPercents > Config.PtvOptTrashholdInPercents;
        }

        private List<Structure> GetCtvs()
        {
            List<Structure> ctvs = GetFilledCTVs();

            if (CtvIsHighResolution(ctvs))
                throw new Exception("Error: CTV is high resolution.");

            Structure ctvAll = StructureSet.GetOrCreateStructure(Config.CtvAllName, dicomType: Config.CtvType);
            MergeStructures(ctvAll, ctvs);
            ctvs.Add(ctvAll);
            return ctvs;
        }

        private List<Structure> GetFilledCTVs()
        {
            try
            {
                return StructureSet.Structures
                    .Where(s => s.Id.ToLower().StartsWith(Config.CtvType))
                    .Where(s => s.IsEmpty == false)
                    .ToList();
            }
            catch (ArgumentNullException)
            {
                throw new Exception("Can not find filled ctv");
            }
        }

        private void MergeStructures(Structure combinedStructure, List<Structure> structures)
        {
            foreach (var structure in structures)
                combinedStructure.SegmentVolume = combinedStructure.Or(structure);
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
