namespace CommonTesting
{
    public static class TestData
    {
        public static readonly bool RecordMode = (
            System.Environment.GetEnvironmentVariable(
                "AAS_CORE_3_0_CLI_SWISS_KNIFE_TESTS_RECORD_MODE"
            )?.ToLower() == "true"
        );

        public static readonly string TestDataDir = (
            System.Environment.GetEnvironmentVariable(
                "AAS_CORE_3_0_CLI_SWISS_KNIFE_TESTS_TEST_DATA_DIR"
            ) ?? throw new System.InvalidOperationException(
                "The path to the test data directory is missing in the environment: " +
                "AAS_CORE_3_0_CLI_SWISS_KNIFE_TESTS_TEST_DATA_DIR")
        );
    }
}