using AIHelpDesk.Application.Common;
using AIHelpDesk.Domain.Enums;

namespace AIHelpDesk.Tests;

public class TicketStateMachineTests
{
    [Theory]
    [InlineData(TicketStatus.Created, TicketStatus.AnalysisInProgress)]
    [InlineData(TicketStatus.Created, TicketStatus.Assigned)]
    [InlineData(TicketStatus.AnalysisInProgress, TicketStatus.Assigned)]
    [InlineData(TicketStatus.AnalysisInProgress, TicketStatus.AnalysisFailed)]
    [InlineData(TicketStatus.AnalysisInProgress, TicketStatus.AwaitingAssignment)]
    [InlineData(TicketStatus.AnalysisFailed, TicketStatus.AnalysisInProgress)]
    [InlineData(TicketStatus.AnalysisFailed, TicketStatus.Assigned)]
    [InlineData(TicketStatus.AwaitingAssignment, TicketStatus.Assigned)]
    [InlineData(TicketStatus.Assigned, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Assigned, TicketStatus.AwaitingAssignment)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed)]
    public void IsAllowed_ReturnsTrue_ForValidTransitions(TicketStatus from, TicketStatus to)
    {
        Assert.True(TicketStateMachine.IsAllowed(from, to));
    }

    [Theory]
    [InlineData(TicketStatus.Closed, TicketStatus.Created)]
    [InlineData(TicketStatus.Closed, TicketStatus.Assigned)]
    [InlineData(TicketStatus.Closed, TicketStatus.Resolved)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Closed)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Assigned)]
    [InlineData(TicketStatus.Resolved, TicketStatus.InProgress)]
    [InlineData(TicketStatus.AwaitingAssignment, TicketStatus.AnalysisInProgress)]
    [InlineData(TicketStatus.Assigned, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Created, TicketStatus.Closed)]
    [InlineData(TicketStatus.AnalysisInProgress, TicketStatus.InProgress)]
    public void IsAllowed_ReturnsFalse_ForInvalidTransitions(TicketStatus from, TicketStatus to)
    {
        Assert.False(TicketStateMachine.IsAllowed(from, to));
    }

    [Fact]
    public void ValidateTransition_Throws_ForInvalidTransition()
    {
        Assert.Throws<InvalidOperationException>(
            () => TicketStateMachine.ValidateTransition(TicketStatus.Closed, TicketStatus.Assigned));
    }

    [Fact]
    public void ValidateTransition_DoesNotThrow_ForValidTransition()
    {
        TicketStateMachine.ValidateTransition(TicketStatus.AwaitingAssignment, TicketStatus.Assigned);
    }
}
