﻿using System;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace Contouring.Tools
{
    public class Cleaner
    {
        private readonly StructuresCropper _cropperByBody;
        private StructureSet StructureSet => Program.StructureSet;

        public Cleaner(CroppersFactory croppersFactory)
        {
            _cropperByBody = croppersFactory.Create(structureByWhichCropName: StructureNames.Body);
        }

        public void RemoveUnnecessaryEmptyStructures()
        {
            Logger.WriteInfo("\tCleaner: RemoveUnnecessaryEmptyStructures");
            Structure[] structuresToRemove = GetUnnecessaryEmptyStructures();

            if (structuresToRemove == null || structuresToRemove.Length == 0)
            {
                Logger.WriteWarning($"There are no unnecessary empty structures.");
                return;
            }

            foreach (var structure in structuresToRemove)
            {
                if (StructureSet.CanRemoveStructure(structure))
                {
                    StructureSet.RemoveStructure(structure);
                    Logger.WriteWarning($"Structure \"{structure.Id}\" has been removed.");
                }
                else
                {
                    Logger.WriteError($"Structure \"{structure.Id}\" can not be removed.");
                }
            }
        }

        public void CropStructures()
        {
            Logger.WriteInfo("\tCleaner: CropStructuresByBody");
            CropByBody(Config.OrgansType, Config.OrganFromBodyMargin);
            CropByBody(Config.CtvType, Config.TargetFromBody);
        }

        private void CropByBody(string dicomType, uint marginInMM)
        {
            var structures = StructureSet.Structures.Where(s => s.DicomType == dicomType);

            foreach (var structure in structures)
            {
                if (structure.IsHighResolution)
                    Logger.WriteError($"Can not crop \"{structure.Id}\" by body because it's high resolution.");
                else
                    CropStructureByBody(structure, marginInMM);
            }
        }

        private void CropStructureByBody(Structure structure, uint marginInMM)
        {
            try
            {
                structure.SegmentVolume = _cropperByBody.Crop(structure, marginInMM, removePartInside: false);
            }
            catch (Exception error)
            {
                Logger.WriteError($"Error during cropping structure \"{structure.Id}\" by body:\n\t{error}\n");
            }
        }

        private Structure[] GetUnnecessaryEmptyStructures()
        {
            return StructureSet.Structures
                .Where(s => s.IsEmpty)
                .Where(s => s.Id.ToLower().Contains(StructureNames.PrvPostfix.ToLower()) == false)
                .Where(s => s.Id.ToLower().Contains(StructureNames.Shoulder.ToLower()) == false)
                .DefaultIfEmpty()
                .ToArray();
        }
    }
}
