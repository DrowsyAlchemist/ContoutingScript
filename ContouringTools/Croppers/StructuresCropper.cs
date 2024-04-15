using Contouring.Extentions;
using System;
using VMS.TPS.Common.Model.API;

namespace Contouring.Tools
{
    public class StructuresCropper
    {
        private readonly Structure _structureByWhichCrop;
        private readonly Structure _marginStructure;

        private StructureSet StructureSet => Program.StructureSet;

        public StructuresCropper(Structure structureByWhichCrop, string marginStructureName)
        {
            _structureByWhichCrop = structureByWhichCrop ?? throw new ArgumentNullException(nameof(structureByWhichCrop));
            _marginStructure = StructureSet.GetOrCreateStructure(marginStructureName);
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
    }
}
