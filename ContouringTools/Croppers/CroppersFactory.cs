using Contouring.Extentions;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace Contouring.Tools
{
    public class CroppersFactory
    {
        private const string MarginStructureName = "Margin structure";
        private static readonly Dictionary<string, StructuresCropper> _croppers = new Dictionary<string, StructuresCropper>();

        private static StructureSet StructureSet => Program.StructureSet;

        public StructuresCropper Create(string structureByWhichCropName)
        {
            if (_croppers.ContainsKey(structureByWhichCropName) == false)
            {
                Structure structure = StructureSet.GetStructure(structureByWhichCropName);
                StructuresCropper cropper = new StructuresCropper(structure, MarginStructureName + "_" + _croppers.Count);
                _croppers.Add(structureByWhichCropName, cropper);
                return cropper;
            }
            return _croppers[structureByWhichCropName];
        }

        public static void RemoveCroppersStructures()
        {
            var marginStructures = StructureSet.Structures.Where(s => s.Id.Contains(MarginStructureName)).ToArray();
            Logger.WriteWarning($"There are {marginStructures.Length} margin structures.");

            foreach (var marginStructure in marginStructures)
                RemoveMarginStructure(marginStructure);
        }

        private static void RemoveMarginStructure(Structure marginStructure)
        {
            if (StructureSet.CanRemoveStructure(marginStructure))
            {
                StructureSet.RemoveStructure(marginStructure);
                Logger.WriteInfo($"{marginStructure.Id} has been removed.");
            }
            else
            {
                Logger.WriteError($"Can not remove {marginStructure.Id}.");
            }
        }
    }
}
