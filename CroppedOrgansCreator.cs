using System;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace ContouringScript
{
    public class CroppedOrgansCreator
    {
        private readonly StructuresCropper _cropperByPTV = null;

        private Application Application => Program.Application;
        private StructureSet StructureSet => Program.StructureSet;

        public CroppedOrgansCreator()
        {
            Structure ptv = StructureSet.GetStructure(Config.PtvAllName);

            if (ptv.IsEmpty)
                throw new Exception($"{Config.PtvAllName} is empty.");

            _cropperByPTV = new StructuresCropper(ptv);
        }

        public void Create()
        {
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
            Application.SaveModifications();
        }

        public void CreateBodyMinusPtv(uint marginInMM)
        {
            try
            {
                Structure body = StructureSet.GetStructure(Config.BodyName);
                Structure bodyMinusPTV = StructureSet.GetOrCreateStructure(Config.BodyMinusPtvName);
                bodyMinusPTV.SegmentVolume = _cropperByPTV.Crop(body, marginInMM);
                Application.SaveModifications();
            }
            catch (Exception)
            {
                Logger.WriteError($"Can not create \"{Config.BodyMinusPtvName}\"");
            }
        }

        private Structure[] GetOrgans()
        {
            return StructureSet.Structures
                .Where(s => s.DicomType == Config.OrgansType)
                .Where(s => s.Id.ToLower().StartsWith(Config.CropPrefix.ToLower()) == false)
                .Where(s => s.Id.ToLower().Equals(Config.BonesName.ToLower()) == false)
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

            croppedOrgan.SegmentVolume = _cropperByPTV.Crop(organ, Config.CropMargin);

            if (IsValidCroppedVolume(organ, croppedOrgan) == false)
                RemoveCroppedOrgan(croppedOrgan);
        }

        private string GetCroppedOrganName(string initialOrganName)
        {
            string croppedOrganName = Config.CropPrefix + initialOrganName + Config.CropPostfix;

            if (croppedOrganName.Length > 16)
                croppedOrganName = croppedOrganName.Substring(0, 10) + croppedOrganName.Substring(croppedOrganName.Length - 6);

            return croppedOrganName;
        }

        private bool IsValidCroppedVolume(Structure initialOrgan, Structure croppedOrgan)
        {
            double croppedVolumeInPercents = croppedOrgan.Volume / initialOrgan.Volume * 100;

            if (croppedVolumeInPercents > Config.CropVolumeTrashholdInPercents)
                return true;

            Logger.WriteWarning($"\"{initialOrgan.Id}\" cropped volume is {croppedVolumeInPercents}. " +
                $"Trashold is {Config.CropVolumeTrashholdInPercents}%.");

            return false;
        }

        private void RemoveCroppedOrgan(Structure croppedOrgan)
        {
            StructureSet.RemoveStructure(croppedOrgan);
            Logger.WriteWarning($"Structure \"{croppedOrgan.Id}\"({croppedOrgan.Volume}) has been removed.");
        }
    }
}
