using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Azure.DurableFunctions.Configuration;
using Azure.DurableFunctions.Models;

namespace Azure.DurableFunctions.SubscriptionClients
{
    public class Api2SubsciptionClient
    {

        [FunctionName(Constants.Api2SubsciptionClientFunction)]
        public async Task Run([ServiceBusTrigger("%TopicName%", "%Subscription1Name%", Connection = "ServiceBusConnectionString")] Message message, MessageReceiver messageReceiver, string lockToken, [DurableClient] IDurableOrchestrationClient client, ILogger logger)
        {
            try
            {
                var messageBody = Encoding.UTF8.GetString(message.Body);
                logger.LogInformation($"message - " + messageBody);
                if (string.IsNullOrWhiteSpace(messageBody))
                {
                    await messageReceiver.DeadLetterAsync(lockToken, "Message content empty.", "Message content is empty.");
                    return;
                }

                var employee = JsonConvert.DeserializeObject<EmployeePayload>(messageBody);
                if (string.IsNullOrWhiteSpace(employee.EmployeeNumber))
                {
                    logger.LogInformation($"Dead Lettering Message.");
                    await messageReceiver.DeadLetterAsync(lockToken, "Missing required fields.", "EmployeeNumber is missing.");//Manually dead letter the message instead of throwing exception
                    logger.LogInformation($"Message Dead Lettered: EmployeeNumber missing.");
                    return;
                }

                string instanceId = await client.StartNewAsync(Constants.Api2OrchestrationFunction, employee);
                logger.LogInformation($"Orchestration Started with ID: {instanceId}");

                var orchestrationStatus = await client.GetStatusAsync(instanceId);

                while (orchestrationStatus.RuntimeStatus == OrchestrationRuntimeStatus.Running || orchestrationStatus.RuntimeStatus == OrchestrationRuntimeStatus.Pending)
                {
                    orchestrationStatus = await client.GetStatusAsync(instanceId);
                    if ((orchestrationStatus.RuntimeStatus == OrchestrationRuntimeStatus.Completed) && ((bool)orchestrationStatus.Output))
                    {
                        logger.LogInformation($"{Constants.Api2SubsciptionClientFunction} completed [Instance ID:{instanceId}]");
                        await messageReceiver.CompleteAsync(lockToken);
                    }
                    else if ((orchestrationStatus.RuntimeStatus == OrchestrationRuntimeStatus.Completed) && (!(bool)orchestrationStatus.Output))
                    {
                        logger.LogInformation($"{Constants.Api2SubsciptionClientFunction} failed [Instance ID:{instanceId}]");
                        await messageReceiver.AbandonAsync(lockToken);
                    }
                }

                if ((orchestrationStatus.RuntimeStatus == OrchestrationRuntimeStatus.Canceled) || (orchestrationStatus.RuntimeStatus == OrchestrationRuntimeStatus.Terminated))
                {
                    logger.LogInformation($"{Constants.Api2SubsciptionClientFunction} either cancelled or terminated [Instance ID:{instanceId}]");
                    await messageReceiver.AbandonAsync(lockToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"{Constants.Api2SubsciptionClientFunction} exception. {ex.StackTrace}");
                await messageReceiver.AbandonAsync(lockToken);
            }
        }
    }
}
