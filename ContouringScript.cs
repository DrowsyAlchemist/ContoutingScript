using System;
using System.Reflection;
using VMS.TPS.Common.Model.API;

[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

[assembly: ESAPIScript(IsWriteable = true)]

namespace Contouring
{
    public class Program
    {
        public static Application Application { get; private set; }
        public static Patient Patient { get; private set; }
        public static StructureSet StructureSet { get; private set; }

        [STAThread]
        public static void Main()
        {
            try
            {
                using (Application app = Application.CreateApplication())
                {
                    Application = app;
                    Patient = OpenPatientById(app);
                    StructureSet = FindStructureSet(Patient);
                    Execute();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
            Console.WriteLine("The program has completed.\nPress any key...");
            Console.ReadKey(true);
        }

        public static void Execute()
        {
            try
            {
                Conture();
            }
            catch (Exception error)
            {
                Logger.WriteError(error.ToString());
            }
            try
            {
                StructuresCropper.RemoveMarginStructures();
            }
            catch { }
            Application.ClosePatient();
            Application.Dispose();
        }

        private static void Conture()
        {
            if (Patient.CanModifyData() == false)
                throw new Exception("The program can not modify data.");

            var cleaner = new Cleaner();
            var targetStructuresCreator = new TargetStructuresCreator();
            var ringCreator = new RingCreator();
            var externalStructureCreatornew = new ExternalStructureCreator();
            var croppedOrgansCreator = new CroppedOrgansCreator();

            Patient.BeginModifications();

            cleaner.CropStructuresByBody();
            targetStructuresCreator.Create();

            if (GetPermition("Do you need PtvOptMinus?"))
                targetStructuresCreator.CreatePtvOptMinus();

            ringCreator.Create();
            externalStructureCreatornew.Create();
            croppedOrgansCreator.Create();

            if (GetPermition("Do you need BodyMinusPtv?"))
                croppedOrgansCreator.CreateBodyMinusPtv(ringCreator.OuterMarginInMM);

            cleaner.RemoveUnnecessaryEmptyStructures();

            Application.SaveModifications();
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
            Console.WriteLine(text);
            var key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.Y:
                    return true;
                case ConsoleKey.N:
                    return false;
                case ConsoleKey.Enter:
                    return false;
                default:
                    GetPermition(text);
                    return false;
            }
        }
    }
}
