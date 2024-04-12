using System;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace ContouringScript
{
    public class StructuresCropper
    {
        private const string MarginStructureName = "Margin structure";
        private static int _instanceCount = 0;
        private readonly Structure _structureByWhichCrop;
        private readonly Structure _marginStructure;

        private static Application Application => Program.Application;
        private static StructureSet StructureSet => Program.StructureSet;

        public StructuresCropper(Structure structureByWhichCrop)
        {
            _structureByWhichCrop = structureByWhichCrop ?? throw new ArgumentNullException(nameof(structureByWhichCrop));
            _instanceCount++;
            _marginStructure = StructureSet.CreateStructure(MarginStructureName + "_" + _instanceCount);
        }

        public static void RemoveMarginStructures()
        {
            var marginStructures = StructureSet.Structures.Where(s => s.Id.Contains(MarginStructureName)).ToArray();
            Logger.WriteWarning($"There are {marginStructures.Length} margin structures.");

            foreach (var marginStructure in marginStructures)
                RemoveMarginStructure(marginStructure);

            Application.SaveModifications();
        }

        public SegmentVolume Crop(Structure structure, uint marginInMM, bool removePartInside = true)
        {
            double cropMargin = marginInMM * (removePartInside ? 1 : -1);
            _marginStructure.SegmentVolume = _structureByWhichCrop.Margin(cropMargin);
            SegmentVolume croppedVolume;

            if (removePartInside)
                croppedVolume = structure.Sub(_marginStructure);
            else
                croppedVolume = structure.And(_marginStructure);

            return croppedVolume;
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
