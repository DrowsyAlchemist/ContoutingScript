using System.Collections.Generic;
using VMS.TPS.Common.Model.API;

namespace Contouring.Extentions
{
    public static class StructureExtentions
    {
        public static SegmentVolume Merge(this Structure combinedStructure, List<Structure> structures)
        {
            SegmentVolume result = combinedStructure.SegmentVolume;

            foreach (var structure in structures)
                result = combinedStructure.Or(structure);

            return result;
        }
    }
}
