using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using System;

namespace GetTableRowCountPlugin
{
    /// <summary>
    /// Custom API Plugin to return the total row count of a Dataverse table.
    /// Registration: Global, Custom API
    /// Input: TargetTable (String)
    /// Output: TotalCount (Long)
    /// </summary>
    public class GetTableRowCountPlugin : PluginBase
    {
        public GetTableRowCountPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(GetTableRowCountPlugin))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;
            var service = localPluginContext.InitiatingUserService;

            // 1. Validate Input Parameters
            if (context.InputParameters.Contains("TargetTable") && context.InputParameters["TargetTable"] is string entityName)
            {
                localPluginContext.Trace($"Retrieving count for table: {entityName}");

                try
                {
                    // 2. Execute RetrieveTotalRecordCountRequest
                    // Note: This queries the metadata snapshot, which is updated approximately every 24 hours.
                    var request = new RetrieveTotalRecordCountRequest
                    {
                        EntityNames = new[] { entityName }
                    };

                    var response = (RetrieveTotalRecordCountResponse)service.Execute(request);

                    // 3. Extract results and assign to Output Parameter
                    if (response.EntityRecordCountCollection.Contains(entityName))
                    {
                        var count = response.EntityRecordCountCollection[entityName];
                        localPluginContext.Trace($"Count found: {count}");
                        context.OutputParameters["TotalCount"] = (long)count;
                    }
                    else
                    {
                        localPluginContext.Trace("Table not found in metadata collection.");
                        context.OutputParameters["TotalCount"] = 0L;
                    }
                }
                catch (Exception ex)
                {
                    localPluginContext.Trace($"Error during execution: {ex.Message}");
                    throw new InvalidPluginExecutionException($"Could not retrieve row count for '{entityName}'. Error: {ex.Message}");
                }
            }
            else
            {
                localPluginContext.Trace("Input parameter 'TargetTable' is missing or invalid.");
            }
        }
    }
}