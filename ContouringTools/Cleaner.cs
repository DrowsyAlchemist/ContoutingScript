using System;
using System.Collections.Generic;
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

        public void CropStructures()
        {
            Logger.WriteInfo("\tCleaner: CropStructuresByBody");
            CropByBody(Config.OrgansType, Config.OrgansIntoBodyMargin);
            CropByBody(Config.CtvType, Config.CtvIntoBodyMargin);
        }

        public void RemoveUnnecessaryEmptyStructures()
        {
            Logger.WriteInfo("\tCleaner: RemoveUnnecessaryEmptyStructures");
            List<Structure> structuresToRemove = GetUnnecessaryEmptyStructures();

            if (structuresToRemove.Count == 0)
            {
                Logger.WriteInfo($"There are no unnecessary empty structures.");
                return;
            }
            while (structuresToRemove.Count > 0)
            {
                Structure structureToRemove = structuresToRemove[0];

                if (StructureSet.CanRemoveStructure(structureToRemove))
                {
                    Logger.WriteInfo($"Empty structure \"{structureToRemove.Id}\" has been removed.");
                    structuresToRemove.RemoveAt(0);
                    StructureSet.RemoveStructure(structureToRemove);
                }
                else
                {
                    Logger.WriteError($"Empty structure \"{structureToRemove.Id}\" can not be removed.");
                }
            }
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

        private List<Structure> GetUnnecessaryEmptyStructures()
        {
            var unnecessaryEmptyStructures = new List<Structure>();

            foreach (Structure structure in StructureSet.Structures)
                if (IsUnnecessary(structure))
                    unnecessaryEmptyStructures.Add(structure);

            return unnecessaryEmptyStructures;
        }

        private bool IsUnnecessary(Structure structure)
        {
            return structure.IsEmpty
                      && structure.Id.Contains(StructureNames.PrvPostfix) == false
                      && structure.Id.Contains(StructureNames.Shoulder) == false;
        }
    }
}
