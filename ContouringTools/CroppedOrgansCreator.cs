using Contouring.Extentions;
using System;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace Contouring.Tools
{
    public class CroppedOrgansCreator
    {
        private readonly CroppersFactory _croppersFactory;
        private uint? _marginFromPtv = null;

        private StructureSet StructureSet => Program.StructureSet;

        public CroppedOrgansCreator(CroppersFactory croppersFactory)
        {
            _croppersFactory = croppersFactory;
        }

        public void Create()
        {
            Logger.WriteInfo("\tCroppedOrgansCreator: Create");

            if (_marginFromPtv == null)
                throw new Exception("Margin in not set.");

            uint margin = (uint)_marginFromPtv;
            Structure[] organs = GetOrgans();
            var cropperByPtv = _croppersFactory.Create(StructureNames.PtvAll);

            for (int i = 0; i < organs.Length; i++)
            {
                try
                {
                    CreateCroppedOrganFrom(organs[i], margin, cropperByPtv);
                }
                catch (Exception error)
                {
                    Logger.WriteError($"Can not crop {organs[i].Id}:\n{error}\n");
                }
            }
        }

        public uint SetMargin()
        {
            if (Config.UseDefaultOrgansMargin)
                _marginFromPtv = Config.CroppedOrgansFromPtvMargin;
            else
                _marginFromPtv = GetMarginFromConsole();

            return (uint)_marginFromPtv;
        }

        public void CreateBodyMinusPtv(uint marginInMM)
        {
            Logger.WriteInfo("\tCroppedOrgansCreator: CreateBodyMinusPtv");

            try
            {
                Structure body = StructureSet.GetStructure(StructureNames.Body);
                Structure bodyMinusPTV = StructureSet.GetOrCreateStructure(StructureNames.SupportivePrefix + StructureNames.BodyMinusPtv);
                bodyMinusPTV.SegmentVolume = _croppersFactory.Create(StructureNames.PtvAll).Crop(body, marginInMM);
            }
            catch (Exception)
            {
                Logger.WriteError($"Can not create \"{StructureNames.BodyMinusPtv}\"");
            }
        }

        public void CropShoulder()
        {
            Logger.WriteInfo("\tCroppedOrgansCreator: CropShoulder");

            try
            {
                Structure shoulder = StructureSet.GetStructure(StructureNames.Shoulder);
                var cropperByPtv = _croppersFactory.Create(StructureNames.PtvAll);
                var cropperByBody = _croppersFactory.Create(StructureNames.Body);
                shoulder.SegmentVolume = cropperByPtv.Crop(shoulder, Config.ShoulderFromPtvMargin);
                shoulder.SegmentVolume = cropperByBody.Crop(shoulder, Config.ShouderIntoBodyMargin, removePartInside: false);
            }
            catch (Exception error)
            {
                Logger.WriteWarning($"Can not crop shoulder.\n" + error);
            }
        }

        private Structure[] GetOrgans()
        {
            return StructureSet.Structures
                .Where(s => s.DicomType == StructureNames.OrgansDicomType)
                .Where(s => s.Id.StartsWith(StructureNames.SupportivePrefix) == false)
                .Where(s => s.Id.Equals(StructureNames.Bones) == false)
                .Where(s => s.Id.ToLower().StartsWith(StructureNames.CtvDicomType.ToLower()) == false)
                .Where(s => s.Id.ToLower().StartsWith(StructureNames.PtvDicomType.ToLower()) == false)
                .ToArray();
        }

        private void CreateCroppedOrganFrom(Structure organ, uint marginFromPtv, StructuresCropper cropper)
        {
            if (organ.IsHighResolution)
                throw new Exception($"{organ.Id} is high resolution.");

            string cropedOrganName = GetCroppedOrganName(organ.Id);
            Structure croppedOrgan = StructureSet.GetOrCreateStructure(cropedOrganName);
            croppedOrgan.SegmentVolume = cropper.Crop(organ, marginFromPtv);

            if (IsValidCroppedVolume(organ, croppedOrgan) == false)
            {
                Logger.WriteInfo($"Structure \"{croppedOrgan.Id}\"({croppedOrgan.Volume:F1} cm2) has been removed.");
                StructureSet.RemoveStructure(croppedOrgan);
            }
        }

        private string GetCroppedOrganName(string initialOrganName)
        {
            string croppedOrganName = StructureNames.SupportivePrefix + initialOrganName + StructureNames.CropPostfix;

            if (croppedOrganName.Length > 16)
                croppedOrganName = croppedOrganName.Substring(0, 10) + croppedOrganName.Substring(croppedOrganName.Length - 6);

            return croppedOrganName;
        }

        private bool IsValidCroppedVolume(Structure initialOrgan, Structure croppedOrgan)
        {
            double croppedVolumeInPercents = (1 - croppedOrgan.Volume / initialOrgan.Volume) * 100;

            Logger.WriteWarning($"\"{initialOrgan.Id}\" cropped volume is {croppedVolumeInPercents:F1} %.\n" +
                $"Threshold is {Config.CropVolumeThresholdInPercents:F1} %.");

            if (croppedVolumeInPercents > Config.CropVolumeThresholdInPercents
                 && croppedVolumeInPercents < (100 - Config.CropVolumeThresholdInPercents))
                return true;

            return false;
        }

        private uint GetMarginFromConsole()
        {
            bool isCorrect = false;
            uint marginInMM = 0;

            while (isCorrect == false)
            {
                Console.Write("Organs from PTV crop margin: ");
                isCorrect = uint.TryParse(Console.ReadLine(), out marginInMM);

                if (marginInMM <= 0)
                {
                    Logger.WriteError("Margin should be positive integer.");
                    isCorrect = false;
                }
            }
            return marginInMM;
        }
    }
}
