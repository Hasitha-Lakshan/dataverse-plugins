/*
    This code demonstrates how to call a Custom API in Dataverse to get the count of rows in a specific table.
    * Note: Ensure that the Custom API "prefix_GetTableCount" is properly registered in your Dataverse environment and that it returns a JSON object with a "TotalCount" property.
*/
const handleCount = async () => {
  const orgUrl = "https://xxxxxxx.api.crm5.dynamics.com"; // URL of the Dataverse environment
  const customApiUniqueName = "prefix_GetTableCount"; // Unique name of the Custom API registered in Dataverse
  const parameters = {
    TargetTable: "prefix_logibridge_bank", // Logical name of the table to count rows for
  };

  const response =
    await MicrosoftDataverseService.PerformUnboundActionWithOrganization(
      orgUrl,
      customApiUniqueName,
      parameters,
    );

  if (response.success && response.data) {
    // 1. Cast response.data to your expected interface
    const data = response.data as { TotalCount: number };

    console.log(`Count: ${data.TotalCount}`);

    /*
      Example output:
      Count: 42
    */
  } else {
    console.error("Action failed:", response.error);
  }
};
