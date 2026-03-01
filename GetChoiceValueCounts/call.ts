/*
    This code demonstrates how to call a Custom API in Dataverse to get the counts of specific choice values for a choice column.
    * Note: Ensure that the Custom API "prefix_GetChoiceValueCounts" is properly registered in your Dataverse environment and that it returns a JSON string with an array of objects containing "OptionValue", "Label", and "Count" properties.
*/
const handleChoiceCounts = async () => {
  const orgUrl = "https://xxxxxxx.api.crm5.dynamics.com"; // URL of the Dataverse environment
  const customApiUniqueName = "prefix_GetChoiceValueCounts"; // Unique name of the Custom API registered in Dataverse
  const parameters = {
    EntityName: "account", // Logical name of the table
    ColumnName: "accountclassificationcode", // Logical name of the choice column
    OptionValues: "1,2,3", // Comma-separated string of option values to count
  };

  const response =
    await MicrosoftDataverseService.PerformUnboundActionWithOrganization(
      orgUrl,
      customApiUniqueName,
      parameters,
    );

  if (response.success && response.data) {
    // Result is returned as JSON string from plugin
    const rawResult = response.data.Result as string;

    const parsed = JSON.parse(rawResult) as {
      OptionValue: number;
      Label: string;
      Count: number;
    }[];

    console.log(parsed);

    /*
      Example:
      [
        { OptionValue: 1, Label: "Prospect", Count: 10 },
        { OptionValue: 2, Label: "Customer", Count: 5 },
        { OptionValue: 3, Label: "Vendor", Count: 0 }
      ]
    */
  } else {
    console.error("Custom API failed:", response.error);
  }
};
