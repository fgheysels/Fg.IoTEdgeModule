namespace Fg.IoTEdgeModule.Tests
{
    public class EnvironmentFixture
    {
        public EnvironmentFixture()
        {
            string envFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            LoadEnvironmentVariables(envFilePath);
        }

        private void LoadEnvironmentVariables(string environmentFile)
        {
            if (!File.Exists(environmentFile))
            {
                return;
            }

            var environmentVars = File.ReadAllLines(environmentFile);

            foreach (var variable in environmentVars)
            {
                var parts = variable.Split('=', count: 2, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 2)
                {
                    Environment.SetEnvironmentVariable(parts[0], parts[1]);
                }
            }

        }
    }
}
