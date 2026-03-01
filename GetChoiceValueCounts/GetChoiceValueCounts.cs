using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace ChoiceCountPlugin
{
    public class GetChoiceCountsWithLabels : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            try
            {
                // ===== Validate Inputs =====
                if (!context.InputParameters.Contains("EntityName") ||
                    !context.InputParameters.Contains("ColumnName") ||
                    !context.InputParameters.Contains("OptionValues"))
                {
                    throw new InvalidPluginExecutionException("Missing required input parameters.");
                }

                string entityName = context.InputParameters["EntityName"].ToString();
                string columnName = context.InputParameters["ColumnName"].ToString();
                string optionValuesRaw = context.InputParameters["OptionValues"].ToString();

                var optionValues = optionValuesRaw
                    .Split(',')
                    .Select(v => int.Parse(v.Trim()))
                    .ToList();

                if (!optionValues.Any())
                    throw new InvalidPluginExecutionException("No valid option values supplied.");

                // ===== FetchXML Aggregation =====
                string valuesCondition = string.Join("", optionValues
                    .Select(v => $"<value>{v}</value>"));

                string fetchXml = $@"
                <fetch aggregate='true'>
                  <entity name='{entityName}'>
                    <attribute name='{columnName}' alias='optionvalue' groupby='true' />
                    <attribute name='{columnName}' alias='count' aggregate='count' />
                    <filter>
                      <condition attribute='{columnName}' operator='in'>
                        {valuesCondition}
                      </condition>
                    </filter>
                  </entity>
                </fetch>";

                var fetchResult = service.RetrieveMultiple(new FetchExpression(fetchXml));

                var counts = optionValues.ToDictionary(v => v, v => 0);

                foreach (var entity in fetchResult.Entities)
                {
                    int option = (int)((AliasedValue)entity["optionvalue"]).Value;
                    int count = Convert.ToInt32(((AliasedValue)entity["count"]).Value);
                    counts[option] = count;
                }

                // ===== Retrieve Metadata for Labels =====
                var request = new RetrieveAttributeRequest
                {
                    EntityLogicalName = entityName,
                    LogicalName = columnName,
                    RetrieveAsIfPublished = true
                };

                var response = (RetrieveAttributeResponse)service.Execute(request);

                var attributeMetadata = response.AttributeMetadata as EnumAttributeMetadata;

                if (attributeMetadata == null)
                    throw new InvalidPluginExecutionException("Column is not a Choice (OptionSet) type.");

                var labelDictionary = attributeMetadata.OptionSet.Options
                    .ToDictionary(
                        o => o.Value.Value,
                        o => o.Label.UserLocalizedLabel?.Label ?? string.Empty
                    );

                // ===== Build Final Result =====
                var finalResult = counts.Select(kvp => new
                {
                    OptionValue = kvp.Key,
                    Label = labelDictionary.ContainsKey(kvp.Key) ? labelDictionary[kvp.Key] : string.Empty,
                    Count = kvp.Value
                }).ToList();

                string jsonResult = System.Text.Json.JsonSerializer.Serialize(finalResult);

                context.OutputParameters["Result"] = jsonResult;
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                throw new InvalidPluginExecutionException("Dataverse fault occurred.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Unexpected error in plugin.", ex);
            }
        }
    }
}