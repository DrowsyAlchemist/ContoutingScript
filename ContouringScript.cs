using System;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using Contouring.Tools;
using Contouring.Extentions;

[assembly: AssemblyVersion("3.0.1.0")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

[assembly: ESAPIScript(IsWriteable = true)]

namespace Contouring
{
    public class Program
    {
        public static Patient Patient { get; private set; }
        public static StructureSet StructureSet { get; private set; }

        [STAThread]
        public static void Main()
        {
            try
            {
                using (Application app = Application.CreateApplication())
                {
                    Patient = OpenPatientById(app);

                    if (Patient.CanModifyData() == false)
                        throw new Exception("The program can not modify data.");

                    Patient.BeginModifications();
                    StructureSet = FindStructureSet(Patient);
                    Execute(app);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
            Console.WriteLine("The program has completed.\nPress any key...");
            Console.ReadKey(true);
        }

        public static void Execute(Application application)
        {
            try
            {
                Conture();
                application.SaveModifications();
            }
            catch (Exception error)
            {
                Logger.WriteError(error.ToString());
            }
            try
            {
                CroppersFactory.RemoveCroppersStructures();
            }
            catch { }
            application.ClosePatient();
            application.Dispose();
        }

        private static void Conture()
        {
            var croppersFactory = new CroppersFactory();

            var cleaner = new Cleaner(croppersFactory);
            cleaner.CropStructures();

            var targetStructuresCreator = new TargetStructuresCreator(croppersFactory);
            targetStructuresCreator.Create();

            if (GetPermition("Do you need PtvOptMinus?"))
                targetStructuresCreator.CreatePtvOptMinus();

            var ringCreator = new RingCreator(croppersFactory);
            ringCreator.Create();

            var externalStructureCreator = new ExternalStructureCreator();
            externalStructureCreator.Create();

            var croppedOrgansCreator = new CroppedOrgansCreator(croppersFactory);
            croppedOrgansCreator.Create();
            croppedOrgansCreator.CropShoulder();

            if (GetPermition("Do you need BodyMinusPtv?"))
                croppedOrgansCreator.CreateBodyMinusPtv(ringCreator.OuterMarginInMM);

            var prvCreator = new PrvCreator(targetStructuresCreator.MarginFromCtv);
            prvCreator.Create();

            cleaner.RemoveUnnecessaryEmptyStructures();
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

        private static StructureSet FindStructureSet(Patient patient)
        {
            foreach (var structureSet in patient.StructureSets)
                if (structureSet.IsValid())
                    return structureSet;

            throw new Exception("Valid StructureSet is not found.");
        }

        private static bool GetPermition(string text)
        {
            Console.WriteLine(text + Config.PermissionDialogText);
            var key = Console.ReadKey();
            Console.WriteLine();

            switch (key.Key)
            {
                case ConsoleKey.Y:
                    return true;
                default:
                    GetPermition(text);
                    return false;
            }
        }
    }
}
