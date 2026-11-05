<!--
    For cXML license agreement information, please see
    http://www.cxml.org/home/license.asp

    $Id$
-->

<!--
    For more information about these elements/entities, see the
    cXML Reference Guide and related documents.
-->

<!--
    Defines the set of known valid cXML digital signature versions.
-->
<!ENTITY % cxml.signatureVersions "(1.0)">

<!--
    cXML envelope.
-->
<!ELEMENT cXML (((Header, (Message | Request)) | Response), ds:Signature*)>
<!--
    @attr version Version of this cXML transmission. Should be less than or equal
		to the version portion of the SYSTEM identifier for this document.

    @attr payloadID A unique identifier for this document.

    @attr timestamp The date and time at which this document was originally created.

    @attr signatureVersion If present, specifies the cXML digital signature version to
        which this document conforms. This implies that the
        ds:Signature element must be present. It is an error if this
        attribute is present and the document does not conform to the
        specified signature version. If absent, the document is not
        signed.

    @attr xml:lang The default locale for all strings (not formatted items such as
        dates, times and numbers) in this document. This attribute will be
        required in a future version of cXML. (Leaving it out is
        deprecated.)
-->
<!ATTLIST cXML
    version    %string;       "&cxml.version;"
    payloadID  %string;       #REQUIRED
    timestamp  %datetime.tz;  #REQUIRED
    signatureVersion %cxml.signatureVersions; #IMPLIED
    xml:lang   %xmlLangCode;  #IMPLIED
>

<!--
	Contains the cXML header.
-->
<!ELEMENT Header (From, To, Sender, (Path, OriginalDocument)?)>

<!--
	Contains the logical source party of the cXML document.
-->
<!ELEMENT From (Credential+, Correspondent?)>

<!--
	Contains the logical destination party of the cXML document.
-->
<!ELEMENT To (Credential+, Correspondent?)>

<!--
	Contains the party that opens the HTTP connection and sends the cXML document.
	When the document is initially sent, Sender and From are the same, However, if the cXML document travels
	through network hubs, the Sender element changes to indicate current sending party.
-->
<!ELEMENT Sender (Credential+, UserAgent)>

<!--
    Path. A list of nodes that records the path taken by a user through
    a punchout chaining scenario.

    If no route node is included then punchout chaining scenario is not executed and only copies are processed.
-->
<!ELEMENT Path (Node+)>

<!--
    A Node is any entity connected to a network.
-->
<!ELEMENT Node (Credential+)>
<!--
    @attr type A node can define itself as a router node or a copy node. Routers
        assume responsibility for the transaction. Copy Nodes request to only
        be aware of the transaction.

    @attr itemDetailsRequired Intermediary Nodes may want to support special operations without
        having to store specific information required to fulfill that operation.
        This attribute tells the previous node to send ItemDetail information
        when performing a PunchOutSetupRequest edit/inspect operation.
-->
<!ATTLIST Node
    type (copy | route) #REQUIRED
    itemDetailsRequired (yes) #IMPLIED
>

<!--
    Identifies the previous document in the situation that a router node
    forwards a message or request on to a more distant node.
-->
<!ELEMENT OriginalDocument EMPTY>
<!--
    @attr payloadID The payloadId of the original document.
-->
<!ATTLIST OriginalDocument
    payloadID %string; #REQUIRED
>

<!--
    A textual string representing who the UserAgent is conducting the cXML
    conversation. Analogous to UserAgent for HTTP conversations.
-->
<!ELEMENT UserAgent (#PCDATA)>

<!--
    DEPRECATED. Do not use this element.
-->
<!ELEMENT DigitalSignature ANY>
<!--
	@attr type DEPRECATED. Do not use.

	@attr encoding DEPRECATED. Do not use.
-->
<!ATTLIST DigitalSignature
    type      %string;  "PK7 self-contained"
    encoding  %string;  "Base64"
>

<!--
    A shared secret. Typically, this is a username/password type of secret
    exchanged through a secure transport before communication takes place.
-->
<!ELEMENT SharedSecret ANY>

<!--
    Represents an identity for a credential.
-->
<!ELEMENT Identity ANY>
<!--
    @attr lastChangedTimestamp When the underlying object last changed in the
		originating system. This is used in cases where the same object (for
		example, a buyer organization) is replicated, and kept synchronized,
		across two systems.
-->
<!ATTLIST Identity
    lastChangedTimestamp  %datetime.tz;  #IMPLIED
>

<!--
    A Credential Message Authentication Code (MAC). This is used in
    situations where one party (the sender) must prove to another (the
    receiver) that it is authenticated by a shared secret with a third
    party trusted by both.

    The MAC should be computed by the trusted third party and
    transferred to the sender. The MAC should be opaque to the sender
    (that is, it should be secure and non-reversible). The MAC should
    use as its inputs enough information to accomplish the following
    goals:

    (1) The MAC must prove to the receiver that it really originated
    with the trusted third party. For example, the MAC could use a shared
    secret between the receiver and the trusted third party as its
    secret key.

    (2) The MAC should be usable only by a certain sender. For example, the
    MAC could authenticate an identifier for the sending organization.

    (3) The MAC should prove that the sender is authorized to send on
    behalf of the From organization. For example, the MAC could authenticate
    an identifier for the From organization.

    (4) The MAC should limit the risk of the MAC being compromised and
    used to impersonate the sender by another party communicating with
    the receiver. For example, the MAC could authenticate an expiration date
    or sequence number.
-->
<!ELEMENT CredentialMac (#PCDATA)>
<!--
    @attr type An implementation-dependent identifier for the exact data
        being authenticated and the method in which it is formatted
        for authentication. Currently the only supported value is
        "FromSenderCredentials".

    @attr algorithm An implementation-dependent identifier for the exact MAC
        algorithm used on the data. Currently the only supported
        value is "HMAC-SHA1-96".

    @attr creationDate The time at which this MAC was issued. The receiver must not
        accept the MAC before this time.

    @attr expirationDate The time at which this MAC expires. The receiver must not
        accept the MAC after this time.
-->
<!ATTLIST CredentialMac
    type           %string;      #REQUIRED
    algorithm      %string;      #REQUIRED
    creationDate   %datetime.tz; #REQUIRED
    expirationDate %datetime.tz; #REQUIRED
>

<!--
	Authentical elements
-->
<!ENTITY % cxml.authentication  "SharedSecret |
                                 DigitalSignature |
                                 CredentialMac"
>
<!--
    A combination of an Identity and authentication element. If the
    authentication element is present, it strongly authenticates who/what
    someone is. The authentication element should not be sent within Message
    documents transported via an end user's browser. One-way communication
    must be authenticated in the transport layer.

	Allows the receiving party to authenticate the sending party. This credential
	allows strong authentication without requiring a public-key end-to-end digital
	certificate infrastructure. Only a user name and password need to be issued
	by the receiving party to allow the sending party to perform Requests.
-->
<!ELEMENT Credential (Identity, (%cxml.authentication;)?)>
<!--
    @attr domain In what domain is this Credential represented?

    @attr type Does this Credential identify a marketplace or one of its member
        companies? A Credential without this attribute describes a member
        company or unaffiliated buying organization.
-->
<!ATTLIST Credential
    domain  %string;      #REQUIRED
    type    (marketplace) #IMPLIED
>

<!--
    Status of a Response or Message. If present, the element content
    describes specifics of a problem.
-->
<!ELEMENT Status (#PCDATA)>
<!--
    @attr code HTTP or cXML-specific status code.

    @attr text Textual version of the status code (not specific issue).

    @attr xml:lang The language in which the text attribute and element content are
		written. This attribute will be required in a future version of
		cXML. (Leaving it out is deprecated.)
-->
<!ATTLIST Status
    code     %uint;        #REQUIRED
    text     %string;      #REQUIRED
    xml:lang %xmlLangCode; #IMPLIED
>

<!--
    Defines a message.

    When Status not present, '<Status code="200" text="OK" />' is implied.
-->
<!ELEMENT Message (Status? %cxml.messages;)>
<!--
	@attr deploymentMode Indicates whether the request is a test request or a production request.
		Allowed values are "production" (default) or "test".

	@attr inReplyTo Specifies to which Message this Message responds. The contents of the inReplyTo
		attribute would be the payloadID of a Message that was received earlier. This would be used
		to construct a two-way conversation with many messages.

	@attr Id This attribute can be used to call out an element and all its children as a target for
		a digital signing.
-->
<!ATTLIST Message
    deploymentMode  (production | test)  "production"
    inReplyTo       %string;  #IMPLIED
    Id              ID        #IMPLIED
>

<!--
	Clients send requests for operations. Only one Request element is allowed for each cXML envelope element, which
	simplifies the server implementations, because no de-multiplexing needs to occur when reading cXML documents.
	The Request element can contain virtually any type of XML data.

	The typical Request elements are:
	<ul>
	<li>OrderRequest</li>
	<li>ProfileRequest</li>
	<li>PunchOutSetupRequest</li>
	<li>StatusUpdateRequest</li>
	<li>GetPendingRequest</li>
	<li>ConfirmationRequest</li>
	<li>ShipNoticeRequest</li>
	<li>ProviderSetupRequest</li>
	<li>PaymentRemittanceRequest</li>
	</ul>
-->
<!ELEMENT Request (%cxml.requests;)>
<!--
	@attr deploymentMode Indicates whether the request is a test request or a production request.
		Allowed values are "production" (default) or "test".

	@attr Id This attribute can be used to call out an element and all its children as a target for
		a digital signing.
-->
<!ATTLIST Request
    deploymentMode  (production | test)  "production"
    Id              ID        #IMPLIED
>

<!--
	Servers send responses to inform clients of the results of operations. Because the result of some requests
	might not have any data, the Response element can optionally contain nothing but a Status element. A Response
	element can also contain any application-level data. During PunchOut for example, the application-level
	data is contained in a PunchOutSetupResponse element.

	The typical Response elements are:
	<ul>
	<li>ProfileResponse</li>
	<li>PunchOutSetupResponse</li>
	<li>GetPendingResponse</li>
	</ul>
-->
<!ELEMENT Response (Status %cxml.responses;)>
<!--
	@attr Id This attribute can be used to call out an element and all its children as a target for
		a digital signing.
-->
<!ATTLIST Response
    Id              ID        #IMPLIED
>

<!--
    This element includes signed cXML-specific details about the
    document being signed. When a cXML document is signed, this
    element must appear inside the first ds:Object element in the
    ds:Signature element.
-->
<!ELEMENT cXMLSignedInfo EMPTY>
<!--
    @attr signatureVersion This specifies the cXML signature version to
		which this document conforms. It is an error if this attribute value
        does not exactly match the value of the signatureVersion
        attribute from the top-level cXML element. It is an error if
        the document does not conform to the specified cXML signature
        version.

    @attr payloadID Specifies the payloadID of the document. It is an error if
        the value of this attribute does not exactly match the value
        of the payloadID attribute from the top-level cXML element.

    @attr Id This identifies this cXMLSignedInfo element for purposes of
        the signature. This attribute must always be present and
        should always have the value "cXMLSignedInfo".
-->
<!ATTLIST cXMLSignedInfo
    signatureVersion    %cxml.signatureVersions;        #REQUIRED
    payloadID           %string;                        #REQUIRED
    Id                  ID                              #REQUIRED
>

<!--
    This element includes the contact information of the organisation
    to/from whom the document is sent/received.

    Routing element includes the document destination system name.
-->
<!ELEMENT Correspondent (Contact+, Routing?, Extrinsic*)>
<!--
    @attr preferredLanguage Optional language which the organization, identified
		by this element, would prefer to use. Refer to the definition of
        xmlLangCode entity.
-->
<!ATTLIST Correspondent
    preferredLanguage     %xmlLangCode;        #IMPLIED
>
