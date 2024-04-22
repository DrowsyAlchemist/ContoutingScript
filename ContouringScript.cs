using System;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using Contouring.Tools;
using Contouring.Extentions;

[assembly: AssemblyVersion("3.0.5.3")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

[assembly: ESAPIScript(IsWriteable = true)]

namespace Contouring
{
    public class Program
    {
        private static Application _application;
        public static Patient Patient { get; private set; }
        public static StructureSet StructureSet { get; private set; }

        [STAThread]
        public static void Main()
        {
            try
            {
                ConfigParser.SetConfig(typeof(StructureNames));
                ConfigParser.SetConfig(typeof(Config));

                using (Application application = Application.CreateApplication())
                {
                    _application = application;
                    Patient = OpenPatientById(application);
                    InitializePatient(Patient);
                    StructureSet = FindValidStructureSet(Patient);
                    Conture();
                    application.SaveModifications();
                }
            }
            catch (Exception e)
            {
                Logger.WriteError(e.ToString());
            }
            CloseSession(_application);
            Console.WriteLine("The program has completed.\nPress any key...");
            Console.ReadKey(true);
        }

        private static void Conture()
        {
            var croppersFactory = new CroppersFactory();
            var cleaner = new Cleaner(croppersFactory);
            var ringCreator = new RingCreator(croppersFactory);
            var targetStructuresCreator = new TargetStructuresCreator(croppersFactory);
            var externalStructureCreator = new ExternalStructureCreator();
            var croppedOrgansCreator = new CroppedOrgansCreator(croppersFactory);
            var prvCreator = new PrvCreator();

            uint ptvMargin = targetStructuresCreator.SetMargin();
            uint organsMarginFromPtv = croppedOrgansCreator.SetMargin();
            ringCreator.SetMargins(out uint inner, out uint ringOuterMargin);

            bool needPtvOptMinus = (Config.OfferPtvOptMinus && GetPermition("Do you need PtvOptMinus?"))
                || (Config.OfferPtvOptMinus == false && Config.CreatePtvOptMinusByDefault);

            bool needBodyMinusPtv = (Config.OfferBodyMinusPtv && GetPermition("Do you need BodyMinusPtv?"))
                || (Config.OfferBodyMinusPtv == false && Config.CreateBodyMinusPtvByDefault);

            Console.WriteLine("Processing...");
            cleaner.CropStructuresByBody();
            targetStructuresCreator.Create();

            if (needPtvOptMinus)
                targetStructuresCreator.CreatePtvOptMinus();

            ringCreator.Create();
            externalStructureCreator.Create();
            croppedOrgansCreator.Create();

            if (needBodyMinusPtv)
                croppedOrgansCreator.CreateBodyMinusPtv(ringOuterMargin);

            if (Config.UseShoulderCropper)
                croppedOrgansCreator.CropShoulder();

            prvCreator.Create(ptvMargin);
            cleaner.RemoveUnnecessaryEmptyStructures();
            croppersFactory.RemoveCroppersStructures();
        }

        private static Patient OpenPatientById(Application application)
        {
            Patient patient = null;

            while (patient == null)
            {
                Console.Write("Patient ID: ");
                string patientId = Console.ReadLine();
                patient = application.OpenPatientById(patientId);

                if (patient == null)
                    Logger.WriteError("Patient is not found.\n");
            }
            return patient;
        }

        private static void InitializePatient(Patient patient)
        {
            if (patient.CanModifyData() == false)
                throw new Exception("The program can not modify data.");

            patient.BeginModifications();
        }

        private static StructureSet FindValidStructureSet(Patient patient)
        {
            foreach (var structureSet in patient.StructureSets)
                if (structureSet.IsValid())
                    return structureSet;

            throw new Exception("Valid StructureSet is not found.");
        }

        private static bool GetPermition(string text)
        {
            while (true)
            {
                Console.WriteLine(text + " (Press Y to accept or N to refuse)");
                var key = Console.ReadKey();
                Console.WriteLine();

                switch (key.Key)
                {
                    case ConsoleKey.Y:
                        return true;
                    case ConsoleKey.N:
                        return false;
                    default:
                        Logger.WriteError("Incorrect input.");
                        break;
                }
            }
        }

        private static void CloseSession(Application application)
        {
            if (application == null)
                return;

            try { application.ClosePatient(); }
            catch { }
            finally { application.Dispose(); }
        }
    }
}
