namespace HoppyRoute.Application.DTOs
{
    /// <summary>
    /// Body for approve/reject. The approver is always the authenticated caller (taken from the
    /// token by the controller); a client-supplied id is neither needed nor read.
    /// </summary>
    public class RouteApprovalRequest
    {
        public string? Notes { get; set; }
    }
}
