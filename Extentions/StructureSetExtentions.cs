using System;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace Contouring.Extentions
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
            catch (Exception error)
            {
                throw new Exception($"Structure \"{name}\" can not be added.\n {error}");
            }
        }

        public static bool IsValid(this StructureSet structureSet)
        {
            foreach (var structure in structureSet.Structures)
            {
                if (structure.Id.ToLower().StartsWith(Config.CtvType.ToLower()) == false)
                    continue;

                if (structure.IsEmpty)
                    continue;

                if (structureSet.CanRemoveStructure(structure) == false)
                {
                    Logger.WriteWarning($"\"{structureSet.Id}\" is not valid: Script is NOT able to remove {structure.Id}.");
                    return false;
                }

                if (structureSet.HasCalculatedPlan())
                {
                    Console.WriteLine($"\"{structureSet.Id}\" is not valid: It has calculated plan.");
                    return false;
                }

                Logger.WriteWarning($"Valid StructureSet is found: \"{structureSet.Id}\"");
                return true;
            }
            return false;
        }

        public static bool Contains(this StructureSet structureSet, string structureName)
        {
            return structureSet.Structures.Any(s => s.Id.Equals(structureName));
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
