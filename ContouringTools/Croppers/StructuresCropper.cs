using Contouring.Extentions;
using System;
using VMS.TPS.Common.Model.API;

namespace Contouring.Tools
{
    public class StructuresCropper
    {
        public readonly Structure MarginStructure;
        private readonly Structure _structureByWhichCrop;

        private StructureSet StructureSet => Program.StructureSet;

        public StructuresCropper(Structure structureByWhichCrop, string marginStructureName)
        {
            _structureByWhichCrop = structureByWhichCrop ?? throw new ArgumentNullException(nameof(structureByWhichCrop));
            MarginStructure = StructureSet.GetOrCreateStructure(marginStructureName);
        }

        public SegmentVolume Crop(Structure structure, uint marginInMM, bool removePartInside = true)
        {
            double cropMargin = marginInMM * (removePartInside ? 1 : -1);
            MarginStructure.SegmentVolume = _structureByWhichCrop.Margin(cropMargin);
            SegmentVolume croppedVolume;

            if (removePartInside)
                croppedVolume = structure.Sub(MarginStructure);
            else
                croppedVolume = structure.And(MarginStructure);

            return croppedVolume;
        }
    }
}
