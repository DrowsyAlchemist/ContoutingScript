using System;
using VMS.TPS.Common.Model.API;

namespace Contouring
{
    public class ExternalStructureCreator
    {
        private Application Application => Program.Application;
        private StructureSet StructureSet => Program.StructureSet;

        public ExternalStructureCreator()
        {
        }

        public void Create()
        {
            Logger.WriteInfo("\tExternalStructureCreator: Create");

            try
            {
                Structure ptv = StructureSet.GetStructure(Config.PtvAllName);
                Structure body = StructureSet.GetStructure(Config.BodyName);

                Structure external = StructureSet.GetOrCreateStructure(Config.ExternalName);
                external.SegmentVolume = ptv.Margin(Config.ExternalOutMargin);
                external.SegmentVolume = external.Sub(body);

                if (external.IsEmpty)
                {
                    StructureSet.RemoveStructure(external);
                    Logger.WriteWarning("External structure is not needed.");
                    return;
                }
                external.SegmentVolume = external.Margin(Config.ExternalMarginIntoBody);
                external.SetAssignedHU(Config.ExternalHU);
                body.SegmentVolume = body.Or(external);

                if (external.CanConvertToHighResolution())
                    external.ConvertToHighResolution();

                Application.SaveModifications();
            }
            catch (Exception error)
            {
                Logger.WriteError($"Error during creating external structure:\n\t{error}\n");
            }
        }
    }
}
