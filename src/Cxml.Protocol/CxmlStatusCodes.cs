namespace Cxml.Protocol;

/// <summary>cXML status codes (see "cXML Basics" in the Getting Started guide) with their default texts.</summary>
public static class CxmlStatusCodes
{
    /// <summary>OK.</summary>
    public const int Ok = 200;
    /// <summary>Accepted.</summary>
    public const int Accepted = 201;
    /// <summary>No Content.</summary>
    public const int NoContent = 204;
    /// <summary>Bad Request: parsed correctly but unacceptable.</summary>
    public const int BadRequest = 400;
    /// <summary>Unauthorized: credentials not recognized.</summary>
    public const int Unauthorized = 401;
    /// <summary>Payment Required.</summary>
    public const int PaymentRequired = 402;
    /// <summary>Forbidden.</summary>
    public const int Forbidden = 403;
    /// <summary>Not Acceptable: typically a parse or validation failure.</summary>
    public const int NotAcceptable = 406;
    /// <summary>Conflict.</summary>
    public const int Conflict = 409;
    /// <summary>Precondition Failed.</summary>
    public const int PreconditionFailed = 412;
    /// <summary>Expectation Failed.</summary>
    public const int ExpectationFailed = 417;
    /// <summary>Not Implemented.</summary>
    public const int NotImplemented = 450;
    /// <summary>Signature Required.</summary>
    public const int SignatureRequired = 475;
    /// <summary>Signature Verification Failed.</summary>
    public const int SignatureVerificationFailed = 476;
    /// <summary>Signature Unacceptable.</summary>
    public const int SignatureUnacceptable = 477;
    /// <summary>Internal Server Error.</summary>
    public const int InternalServerError = 500;
    /// <summary>Unable to reach cXML server.</summary>
    public const int UnableToReachServer = 550;
    /// <summary>Unable to forward request.</summary>
    public const int UnableToForward = 551;
    /// <summary>Temporary server error.</summary>
    public const int TemporaryServerError = 560;

    private static readonly Dictionary<int, string> Texts = new()
    {
        [Ok] = "OK",
        [Accepted] = "Accepted",
        [NoContent] = "No Content",
        [BadRequest] = "Bad Request",
        [Unauthorized] = "Unauthorized",
        [PaymentRequired] = "Payment Required",
        [Forbidden] = "Forbidden",
        [NotAcceptable] = "Not Acceptable",
        [Conflict] = "Conflict",
        [PreconditionFailed] = "Precondition Failed",
        [ExpectationFailed] = "Expectation Failed",
        [NotImplemented] = "Not Implemented",
        [SignatureRequired] = "Signature Required",
        [SignatureVerificationFailed] = "Signature Verification Failed",
        [SignatureUnacceptable] = "Signature Unacceptable",
        [InternalServerError] = "Internal Server Error",
        [UnableToReachServer] = "Unable to reach cXML server",
        [UnableToForward] = "Unable to forward request",
        [TemporaryServerError] = "Temporary server error",
    };

    /// <summary>All status codes that have a registered default text.</summary>
    public static IReadOnlyCollection<int> KnownCodes => Texts.Keys;

    /// <summary>Returns the default text for a code, or a generic fallback for unknown codes.</summary>
    public static string DefaultText(int code) =>
        Texts.TryGetValue(code, out var text) ? text : code is >= 200 and < 300 ? "OK" : "Error";

    /// <summary>True for 2xx codes: the request succeeded and a payload may accompany the response.</summary>
    public static bool IsSuccess(int code) => code is >= 200 and < 300;

    /// <summary>True for 4xx codes: a permanent error; the same request will not succeed if retried.</summary>
    public static bool IsPermanent(int code) => code is >= 400 and < 500;

    /// <summary>True for 5xx codes: a transient error; the client may retry later.</summary>
    public static bool IsTransient(int code) => code is >= 500 and < 600;
}
