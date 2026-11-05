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
    The OrderReference element provides a clear reference to a prior
    OrderRequest document. While the contained DocumentReference provides
    an unambiguous reference, the additional attributes of the
    OrderReference may allow the ConfirmationRequest and ShipNoticeRequest
    to be viewed independently.
-->
<!ELEMENT OrderReference (DocumentReference)>
<!--
    @attr orderID The buyer system orderID for this request. Basically, what the PO
        number is today. If present, must be copied directly from the
        referenced OrderRequest document's OrderRequestHeader.

    @attr orderDate The date and time the order request was created. If present, must
        be copied directly from the referenced OrderRequest document's
        OrderRequestHeader.
-->
<!ATTLIST OrderReference
    orderID              %string;              #IMPLIED
    orderDate            %datetime.tz;         #IMPLIED
>

<!--
    Defines the ID of an order known to the buyer system.
-->
<!ELEMENT OrderIDInfo (IdReference*)>
<!--
    @attr orderID The id of an order known to the buyer system. Basically,
        what the PO number is today.

    @attr orderDate The date and time the order was created.
-->
<!ATTLIST OrderIDInfo
    orderID              %string;               #REQUIRED
    orderDate            %datetime.tz;          #IMPLIED
>

<!--
    Defines information related to an order.

    OrderReference: The reference to the order being paid.

    OrderIDInfo: The buyer system order id of the order.
-->
<!ELEMENT OrderInfo (OrderReference | OrderIDInfo)>

<!--
    The InvoiceReference element provides a clear reference to a prior
    InvoiceDetailRequest document.
-->
<!ELEMENT InvoiceReference (DocumentReference)>
<!--
    @attr invoiceID The supplier system invoiceID for this request. Basically, what
        the Invoice number is today. If present, must be copied directly from the
		referenced InvoiceDetailRequest document's InvoiceDetailRequestHeader.

    @attr invoiceDate The invoice date.
-->
<!ATTLIST InvoiceReference
    invoiceID              %string;               #IMPLIED
    invoiceDate            %datetime.tz;          #IMPLIED
>

<!--
    Defines the ID of an invoice known to the supplier system.
-->
<!ELEMENT InvoiceIDInfo EMPTY>
<!--
    @attr invoiceID The id of an invoice known to the supplier system.

    @attr invoiceDate The invoice date.
-->
<!ATTLIST InvoiceIDInfo
    invoiceID              %string;               #REQUIRED
    invoiceDate            %datetime.tz;          #IMPLIED
>

<!--
    Defines the ID of a PaymentProposalRequest known to the supplier and buyer system.
-->
<!ELEMENT PaymentProposalIDInfo EMPTY>
<!--
    @attr paymentProposalID The ID of a PaymentProposalRequest known to the buyer and supplier system.
-->
<!ATTLIST PaymentProposalIDInfo
    paymentProposalID      %string;               #REQUIRED
>

<!--
    Defines the ID of a master agreement known to the buyer system.
    In InvoiceDetailRequest, this element identifies the master agreement
    of the release order to be invoiced.

    IdReference: Additional reference ID for MasterAgreement.
-->
<!ELEMENT MasterAgreementIDInfo (IdReference*)>
<!--
    @attr agreementID The id of a master agreement known to the buyer system. Basically,
        what the master agreement number is today.

    @attr agreementDate The date and time the master agreement request was created.

    @attr agreementType The type of the master agreement being referenced, for example: Scheduling Agreement
-->
<!ATTLIST MasterAgreementIDInfo
    agreementID              %string;               #REQUIRED
    agreementDate            %datetime.tz;          #IMPLIED
    agreementType            (scheduling_agreement) #IMPLIED

>

<!--
    Defines a reference to an earlier MasterAgreementRequest document.
    In InvoiceDetailRequest, this element identifies the master agreement
    of the release order to be invoiced.

    DocumentReference: The reference to an earlier MasterAgreementRequest document.
-->
<!ELEMENT MasterAgreementReference (DocumentReference)>
<!--
    @attr agreementID The id of a master agreement known to the buyer system. Basically,
        what the master agreement number is today.

    @attr agreementDate The date and time the master agreement request was created.

    @attr agreementType The type of the master agreement being referenced, for example: Scheduling Agreement.
-->
<!ATTLIST MasterAgreementReference
    agreementID              %string;               #IMPLIED
    agreementDate            %datetime.tz;          #IMPLIED
    agreementType            (scheduling_agreement) #IMPLIED
>

<!--
	Defines a reference to an earlier TransportRequest document.

	DocumentReference: The reference to an earlier TransportRequest document.
-->
<!ELEMENT TransportReference (DocumentReference)>
<!--
    @attr requestID The id of a transport request known to the system of the party that
        created the TransportRequest. Basically, what the transport request
        number is today.

	@attr requestDate The date and time the transport request was created.
-->
<!ATTLIST TransportReference
	requestID	%string;		#IMPLIED
	requestDate	%datetime.tz;	#IMPLIED
>

<!--
    Defines the ID of a transport request known to the system of the party
    that created the TransportRequest.

    IdReference: Additional reference ID for TransportRequest.
-->
<!ELEMENT TransportIDInfo (IdReference*)>
<!--
    @attr requestID The id of a transport request known to the system of the party that
        created the TransportRequest. Basically, what the transport request
        number is today.

    @attr requestDate The date and time the transport request was created.
-->
<!ATTLIST TransportIDInfo
    requestID              %string;               #IMPLIED
    requestDate            %datetime.tz;          #IMPLIED
>

<!--
    Identifies the carrier who will transport a shipment.
-->
<!ELEMENT CarrierIdentifier (#PCDATA)>         <!-- string -->
<!--
    domain
        Domain in which this value has meaning. Recognized domains
        include:
        <ul>
		<li>companyName - The legal name for this company. In some cases, this
            could also be provided in a Contact element with role
            "carrierCorporate". That option should be reserved for cases
            in which additional detail about the carrier appears in this
            element.</li>
		<li>SCAC - Standard Carrier Alpha Code (see
            http://users.erols.com/nmfta/Codes.htm).</li>
        <li>IATA - International Air Transport Association (see
            http://www.iata.org).</li>
        <li>AAR - Association of American Railroads (see http://www.aar.org/).</li>
        <li>UIC - International Union of Railways (see
            http://www.uic.asso.fr/).</li>
        <li>EAN - European Article Numbering (see http://www.ean-ucc.org/).</li>
        <li>DUNS - D&B's Data Universal Numbering System (see
            http://www.dnb.com/dnbhome.htm).</li>
		</ul>
-->
<!ATTLIST CarrierIdentifier
    domain               %string;              #REQUIRED
>

<!--
    Identifier that appears on a shipment and through which additional
    detail about the shipment may be retrieved. Defined by the carrier.
    Has meaning in the domain described by the CarrierIdentifier values.
    Therefore, CarrierIdentifier and ShipmentIdentifier should normally
    be used together.

    Conceptually, this is a tracking number. Different carriers have
    different names for shipment identifiers. Some call it a way bill
    number, others call it a pro number, and still others call it a bill of
    lading. They all represent tracking numbers.
-->
<!ELEMENT ShipmentIdentifier (#PCDATA)>        <!-- string -->
<!--
    @attr domain Specifies more precisely what kind of identifier this is.
        Likely values include trackingNumber, billOfLading.

    @attr trackingNumberDate The date when the logistics provider intimates the tracking number
        for this shipment. This date is used to find out the efficiency of the
        supply chain process.

    @attr trackingURL Carrier URL that can be used to track the shipment in conjunction
		with the tracking number
-->
<!ATTLIST ShipmentIdentifier
    domain                %string;              #IMPLIED
    trackingNumberDate    %datetime.tz;         #IMPLIED
    trackingURL           %URL;                 #IMPLIED>

<!--
    One language-specific string for the creator of an IdReference,
    for example, IRS, BofA, UPS, Cisco, and so on.
-->
<!ELEMENT Creator (#PCDATA)>              <!-- string -->
<!--
    @attr xml:lang The language or locale in which the name of the creator is written.
-->
<!ATTLIST Creator
   xml:lang              %xmlLangCode;         #REQUIRED
>

<!--
    Defines an ID reference. Within the application context (for example,
    a certain pair of buyer and supplier), the (identifier, domain) pair
    should be unique.

    Creator: The creator of this IdReference, for example, BofA, UPS, Cisco, and so on.

    Description: Textual description of the IdReference. For human readability.
-->
<!ELEMENT IdReference (Creator?, Description?)>
<!--
    @attr identifier The unique identifier of the IdReference within the domain.

    @attr domain The domain of the IdReference. Supported values depend on your procurement application. Possible values:
		<ul>
		<li>1099ID</li>
		<li>abaRoutingNumber</li>
		<li>accountID</li>
		<li>accountPayableID</li>
		<li>accountReceivableID</li>
		<li>bankAccountID</li>
		<li>bankBranchID</li>
		<li>bankNationalID</li>
		<li>bankRoutingID</li>
		<li>buyerLocationID</li>
		<li>companyRegistrationNumber</li>
		<li>courtRegisterID</li>
		<li>creditorRefID</li>
		<li>documentName</li>
		<li>federalTaxID</li>
		<li>governmentNumber</li>
		<li>gstID</li>
		<li>ibanID</li>
		<li>isoBicID</li>
		<li>iso6523</li>
		<li>provincialTaxID</li>
		<li>stateTaxID</li>
		<li>storageLocationID</li>
		<li>supplierReference</li>
		<li>swiftID</li>
		<li>taxExemptionID</li>
		<li>unloadingPointID</li>
		<li>vatID</li>
        <li>ID</li>
		</ul>

		Note: supplierTaxID is deprecated and will be treated as federalTaxID.
-->
<!ATTLIST IdReference
    identifier      %string;                         #REQUIRED
    domain          %string;                         #REQUIRED
>

<!--
    Defines supplier sales order information related to an order.

    This is also used in PunchOutOrderMessage to indicate that the
    supplier has created an order associated with the punchout order message.
    The buyer can later cancel the sales order by sending an OrderRequest
    of type "delete" that refers to the sales order by including the
    SupplierOrderInfo element in the OrderRequestHeader
-->
<!ELEMENT SupplierOrderInfo EMPTY>
<!--
    @attr orderID Supplier sales order id of this order.

    @attr orderDate The date and time the sales order was created.
-->
<!ATTLIST SupplierOrderInfo
    orderID              %string;               #REQUIRED
    orderDate          %datetime.tz;      #IMPLIED
>

<!--
    The ShipNoticeReference element provides a clear reference to a prior
    ShipNoticeRequest document. While the contained DocumentReference provides
    an unambiguous reference, the additional attributes of the
    ShipNoticeReference may allow the ReceiptRequest to be viewed independently.

    DocumentReference: The reference to an earlier ShipNoticeRequest.
-->
<!ELEMENT ShipNoticeReference (DocumentReference)>
<!--
    @attr shipNoticeID Unique identifier of the referenced ShipNoticeRequest.

    @attr shipNoticeDate Date and time of the referenced ShipNoticeRequest.
-->
<!ATTLIST ShipNoticeReference
	shipNoticeID %string; #IMPLIED
	shipNoticeDate %datetime.tz; #IMPLIED
>

<!--
    Defines ID of a Ship Notice known to the Buyer system.

    IdReference: Additional reference ID for shipment related IDs, for example,
		DispatchAdviceID, ReceivingAdviceID, DeliveryNoteId, ProofOfDeliveryID.
-->
<!ELEMENT ShipNoticeIDInfo (IdReference*)>
<!--
    @attr shipNoticeID Unique identifier of the referenced ShipNoticeRequest.

    @attr shipNoticeDate Date and time of the referenced ShipNoticeRequest.
-->
<!ATTLIST ShipNoticeIDInfo
    shipNoticeID              %string;               #REQUIRED
    shipNoticeDate            %datetime.tz;          #IMPLIED
>

<!--
    Provides a clear reference to a prior OrderStatusRequest document.
-->
<!ELEMENT OrderStatusRequestReference (DocumentReference)>
<!--
    @attr orderStatusRequestID The buyer system orderStatusRequestID for this request. If present, must be copied
        directly from the referenced OrderStatusRequest document's
        OrderStatusRequestHeader.

    @attr orderStatusRequestDate The order status request date.
-->
<!ATTLIST OrderStatusRequestReference
    orderStatusRequestID              %string;               #IMPLIED
    orderStatusRequestDate            %datetime.tz;          #IMPLIED
>

<!--
    Defines the ID of an order status request known to the buyer system.
-->
<!ELEMENT OrderStatusRequestIDInfo EMPTY>
<!--

    @attr orderStatusRequestID The id of an order status request known to the buyer system.

    @attr orderStatusRequestDate The order status request date.
-->
<!ATTLIST OrderStatusRequestIDInfo
    orderStatusRequestID              %string;               #REQUIRED
    orderStatusRequestDate            %datetime.tz;          #IMPLIED
>

<!--
    The ReceiptReference element provides a clear reference to a prior
    ReceiptRequest document.
-->
<!ELEMENT ReceiptReference (DocumentReference) >
<!--
    @attr receiptID The buyer system receiptID for this request.
        If present, must be copied directly from the
        referenced ReceiptRequest document's ReceiptRequestHeader.

    @attr receiptDate The date and time the receipt request was created. If present, must
        be copied directly from the referenced ReceiptRequest document's
        RequestRequestHeader.
-->
<!ATTLIST ReceiptReference
    receiptID              %string;              #IMPLIED
    receiptDate            %datetime.tz;         #IMPLIED
>

<!--
    Defines the ID of a receipt known to the buyer system.
-->
<!ELEMENT ReceiptIDInfo (IdReference*)>
<!--
    @attr receiptID The id of an receipt known to the buyer system.

    @attr receiptDate The date and time the order was created.
-->
<!ATTLIST ReceiptIDInfo
    receiptID              %string;               #REQUIRED
    receiptDate            %datetime.tz;          #IMPLIED
>

<!--
    Defines the ID of a quality inspection request document.
-->
<!ELEMENT QualityInspectionRequestReference (DocumentReference?)>
<!--
    @attr inspectionID The id of a quality inspection request document.

    @attr inspectionDate The date and time the quality inspection request document was created.
-->
<!ATTLIST QualityInspectionRequestReference
    inspectionID              %string;               #IMPLIED
    inspectionDate           %datetime.tz;          #IMPLIED
>

<!--
    Defines the ID of a quality inspection result document.
-->
<!ELEMENT QualityInspectionResultReference (DocumentReference?)>
<!--
    @attr inspectionID The id of a quality inspection result document.

    @attr inspectionDate The date and time the quality inspection result document was created.
-->
<!ATTLIST QualityInspectionResultReference
    inspectionID              %string;               #IMPLIED
    inspectionDate           %datetime.tz;          #IMPLIED
>

<!--
    Identifies legal entity in the external system.
-->
<!ELEMENT LegalEntity (IdReference)>

<!--
    Identifies Purchase Unit or Purchase Group in the external system.

    IdReference: Contains a unique identification reference for the Purchase
		Unit or Purchase Group.
-->
<!ELEMENT OrganizationalUnit (IdReference)>

<!--
    Defines the correspondent routing destination.
 -->
<!ELEMENT Routing EMPTY>

<!--
    @attr destination Name of the destination. Possible values are peppol or fieldglass
 -->

<!ATTLIST Routing
    destination      (peppol | fieldglass)    #REQUIRED
>
