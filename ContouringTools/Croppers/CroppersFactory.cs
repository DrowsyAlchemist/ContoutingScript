using Contouring.Extentions;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace Contouring.Tools
{
    public class CroppersFactory
    {
        private const string MarginStructureName = "MarginStr";
        private readonly Dictionary<string, StructuresCropper> _croppers = new Dictionary<string, StructuresCropper>();

        private StructureSet StructureSet => Program.StructureSet;

        public StructuresCropper Create(string structureByWhichCropName)
        {
            if (_croppers.ContainsKey(structureByWhichCropName) == false)
            {
                Structure structure = StructureSet.GetStructure(structureByWhichCropName);
                var cropper = new StructuresCropper(structure, StructureNames.SupportivePrefix + MarginStructureName + "_" + _croppers.Count);
                _croppers.Add(structureByWhichCropName, cropper);
                return cropper;
            }
            return _croppers[structureByWhichCropName];
        }

        public void RemoveCroppersStructures()
        {
            Logger.WriteInfo("CroppersFactory: RemoveCroppersStructures");
            var marginStructures = StructureSet.Structures.Where(s => s.Id.Contains(MarginStructureName)).ToList();
            Logger.WriteWarning($"There are {marginStructures.Count} margin structures.");

            while (marginStructures.Count > 0)
            {
                Structure marginStructure = marginStructures[0];

                if (StructureSet.CanRemoveStructure(marginStructure))
                {
                    Logger.WriteInfo($"{marginStructure.Id} has been removed.");
                    marginStructures.RemoveAt(0);
                    StructureSet.RemoveStructure(marginStructure);
                }
                else
                {
                    Logger.WriteError($"Can not remove {marginStructure.Id}.");
                }
            }
        }
    }
}
