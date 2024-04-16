using Contouring.Extentions;
using System;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace Contouring.Tools
{
    public class CroppedOrgansCreator
    {
        private readonly StructuresCropper _cropperByPTV;
        private readonly StructuresCropper _cropperByBody;

        private StructureSet StructureSet => Program.StructureSet;

        public CroppedOrgansCreator(CroppersFactory croppersFactory)
        {
            _cropperByPTV = croppersFactory.Create(StructureNames.PtvAll);
            _cropperByBody = croppersFactory.Create(StructureNames.Body);
        }

        public void Create()
        {
            Logger.WriteInfo("\tCroppedOrgansCreator: Create");
            Structure[] organs = GetOrgans();

            for (int i = 0; i < organs.Length; i++)
            {
                try
                {
                    CreateCroppedOrganFrom(organs[i]);
                }
                catch (Exception error)
                {
                    Logger.WriteError($"Can not crop {organs[i].Id}:\n{error}\n");
                }
            }
        }

        public void CreateBodyMinusPtv(uint marginInMM)
        {
            Logger.WriteInfo("\tCroppedOrgansCreator: CreateBodyMinusPtv");

            try
            {
                Structure body = StructureSet.GetStructure(StructureNames.Body);
                Structure bodyMinusPTV = StructureSet.GetOrCreateStructure(StructureNames.SupportivePrefix + StructureNames.BodyMinusPtv);
                bodyMinusPTV.SegmentVolume = _cropperByPTV.Crop(body, marginInMM);
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
                _cropperByPTV.Crop(shoulder, Config.OrgansCropMarginFromPtv);
                shoulder.SegmentVolume = _cropperByPTV.Crop(shoulder, Config.ShouderMarginFromPtv);
                shoulder.SegmentVolume = _cropperByBody.Crop(shoulder, Config.ShouderMarginFromBody, removePartInside: false);
            }
            catch (Exception error)
            {
                Logger.WriteWarning($"Can not crop shoulder.\n" + error);
            }
        }

        private Structure[] GetOrgans()
        {
            return StructureSet.Structures
                .Where(s => s.DicomType == Config.OrgansType)
                .Where(s => s.Id.StartsWith(StructureNames.SupportivePrefix) == false)
                .Where(s => s.Id.Equals(StructureNames.Bones) == false)
                .Where(s => s.Id.ToLower().StartsWith(Config.CtvType.ToLower()) == false)
                .Where(s => s.Id.ToLower().StartsWith(Config.PtvType.ToLower()) == false)
                .ToArray();
        }

        private void CreateCroppedOrganFrom(Structure organ)
        {
            string cropedOrganName = GetCroppedOrganName(organ.Id);
            Structure croppedOrgan = StructureSet.GetOrCreateStructure(cropedOrganName);

            if (organ.IsHighResolution)
                throw new Exception($"{organ.Id} is high resolution.");

            croppedOrgan.SegmentVolume = _cropperByPTV.Crop(organ, Config.OrgansCropMarginFromPtv);

            if (IsValidCroppedVolume(organ, croppedOrgan) == false)
            {
                Logger.WriteInfo($"Structure \"{croppedOrgan.Id}\"({croppedOrgan.Volume} cm2) has been removed.");
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

            Logger.WriteWarning($"\"{initialOrgan.Id}\" cropped volume is {croppedVolumeInPercents}.\n" +
                $"Threshold is {Config.CropVolumeThresholdInPercents}%.");

            if (croppedVolumeInPercents > Config.CropVolumeThresholdInPercents)
                return true;

            return false;
        }
    }
}
