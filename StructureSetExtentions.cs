using System;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace Contouring
{
    static class StructureSetExtentions
    {
        public static Structure GetOrCreateStructure(this StructureSet structureSet, string name, string dicomType = "ORGAN")
        {
            Structure structure;
            try
            {
                structure = structureSet.GetStructure(name);
            }
            catch
            {
                structure = structureSet.CreateStructure(name, dicomType);
            }
            return structure;
        }

        public static Structure GetStructure(this StructureSet structureSet, string name)
        {
            try
            {
                Structure structure = structureSet.Structures.Where(s => s.Id.ToLower() == name.ToLower()).Single();
                Logger.WriteInfo($"Structure \"{name}\" is found.");
                return structure;
            }
            catch (Exception)
            {
                throw new Exception($"Structure \"{name}\" is not found.");
            }
        }

        public static Structure CreateStructure(this StructureSet structureSet, string name, string dicomType = "ORGAN")
        {
            if (name.Length > 16)
                name = name.Substring(0, 10) + name.Substring(name.Length - 6);

            try
            {
                Structure structure = structureSet.AddStructure(dicomType, name);
                Logger.WriteInfo($"Structure \"{name}\" has been added.");
                return structure;
            }
            catch
            {
                throw new Exception($"Structure \"{name}\" can not be added.");
            }
        }

        public static bool IsValid(this StructureSet structureSet)
        {
            foreach (var structure in structureSet.Structures)
            {
                if (structure.Id.ToLower().StartsWith("ctv")
                        && structure.IsEmpty == false
                        && structureSet.CanRemoveStructure(structure)
                        && structureSet.HasCalculatedPlan() == false)
                {
                    Logger.WriteInfo($"Valid StructureSet: \"{structureSet.Id}\"");
                    return true;
                }
            }
            return false;
        }

        private static bool HasCalculatedPlan(this StructureSet structureSet)
        {
            foreach (var course in structureSet.Patient.Courses)
                foreach (var plan in course.ExternalPlanSetups)
                    if (plan.IsDoseValid && plan.StructureSet == structureSet)
                        return true;

            return false;
        }
    }
}
