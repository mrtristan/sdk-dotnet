namespace Temporalio.Tests.Common;

using Google.Protobuf;
using Temporalio.Api.Enums.V1;
using Temporalio.Api.History.V1;
using Temporalio.Common;
using Xunit;
using Xunit.Abstractions;

public class WorkflowHistoryTests : TestBase
{
    public WorkflowHistoryTests(ITestOutputHelper output)
        : base(output)
    {
    }

    [Fact]
    public void FromJson_InvalidEnums_GetFixed()
    {
        // Create a history JSON with some enums
        var history = new History();
        history.Events.Add(new HistoryEvent()
        {
            EventType = EventType.RequestCancelExternalWorkflowExecutionFailed,
            RequestCancelExternalWorkflowExecutionFailedEventAttributes = new()
            {
                Cause = CancelExternalWorkflowExecutionFailedCause.ExternalWorkflowExecutionNotFound,
            },
        });
        history.Events.Add(new HistoryEvent()
        {
            WorkflowExecutionStartedEventAttributes = new()
            {
                TaskQueue = new() { Kind = TaskQueueKind.Sticky },
            },
        });
        history.Events.Add(new HistoryEvent()
        {
            WorkflowExecutionStartedEventAttributes = new()
            {
                ContinuedFailure = new()
                {
                    Cause = new()
                    {
                        ChildWorkflowExecutionFailureInfo = new()
                        {
                            RetryState = RetryState.InProgress,
                        },
                    },
                    TimeoutFailureInfo = new()
                    {
                        TimeoutType = TimeoutType.Heartbeat,
                    },
                },
            },
        });
        var historyJson = JsonFormatter.Default.Format(history);
        WorkflowHistory AssertHistoryJson()
        {
            var newHistory = WorkflowHistory.FromJson("some-workflow-id", historyJson!);
            Assert.Equal(history.Events, newHistory.Events);
            return newHistory;
        }

        // Confirm JSON conversion does work on proper proto form and proto could even convert back
        AssertHistoryJson();
        JsonParser.Default.Parse<History>(historyJson);

        // Replace known enums with bad values
        void Replace(string prevVal, string newVal)
        {
            var newHistoryJson = historyJson!.Replace(prevVal, newVal);
            Assert.NotEqual(historyJson, newHistoryJson);
            historyJson = newHistoryJson;
        }
        Replace(
            "EVENT_TYPE_REQUEST_CANCEL_EXTERNAL_WORKFLOW_EXECUTION_FAILED",
            "RequestCancelExternalWorkflowExecutionFailed");
        Replace(
            "CANCEL_EXTERNAL_WORKFLOW_EXECUTION_FAILED_CAUSE_EXTERNAL_WORKFLOW_EXECUTION_NOT_FOUND",
            "ExternalWorkflowExecutionNotFound");
        Replace(
            "TASK_QUEUE_KIND_STICKY",
            "Sticky");
        Replace(
            "RETRY_STATE_IN_PROGRESS",
            "InProgress");
        Replace(
            "TIMEOUT_TYPE_HEARTBEAT",
            "Heartbeat");

        // Confirm proto history JSON would fail
        var protoExc = Assert.Throws<InvalidProtocolBufferException>(
            () => JsonParser.Default.Parse<History>(historyJson));
        Assert.Contains("Invalid enum value", protoExc.Message);

        // But that our history parser succeeds
        var newHistory = AssertHistoryJson();
    }
}