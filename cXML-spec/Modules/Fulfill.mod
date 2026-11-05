<!--
    For cXML license agreement information, please see
    http://www.cxml.org/home/license.asp
    
    $Id$
-->

<!-- ====
    Allow this module to be built separately from any other DTD file.
==== -->

<!ENTITY % cxml.requests "(ConfirmationRequest |
                           ShipNoticeRequest |
                           ServiceEntryRequest |
                           CopyRequest |
                           SalesOrderRequest |
			   TimeCardInfoRequest |
			   ReceiptRequest |
               OrderStatusRequest |
                           TimeCardRequest |
                           ComponentConsumptionRequest)"
>

<!ENTITY % cxml.responses "">

<!ENTITY % cxml.messages ",(ProductReplenishmentMessage)">

<!-- ====
    Common to both of the documents listed below.
==== -->


<!-- ====
    Confirmation* transaction forwards confirmation data in the Request.
    No specific Response document is required for this transaction.
    Servers must respond to a ConfirmationRequest with a generic Response
    document.
==== -->

<!-- OrderStatusRequest Elements -->
<!--
    Definition of an order status request. This is the data that is sent to the supplier to ask 
    for information regarding a previously sent PO such as the status of the order, delivery date 
    or current location of a shipped product.
-->
<!ELEMENT OrderStatusRequest (OrderStatusRequestHeader, OrderStatusRequestItem*)>
    
<!-- OrderStatusRequestHeader Elements -->
<!--
    Header of an order status request. It contains the reference to the orderStatusRequest 
    (OrderStatusRequestID, OrderStatusRequestDate) from the buyer side.
	
	OrderReference | OrderIDInfo: The reference to the purchase order. You can specify either 
		the OrderReference or OrderIDInfo information.
		
	Contact: The contact information of the buyer.
-->
<!ELEMENT OrderStatusRequestHeader ((OrderReference | OrderIDInfo),Contact+, Comments?, Extrinsic*)>
<!--
    @attr orderStatusRequestID The buyer system ID for this status request. This is an internal 
		buyer unique number.

    @attr orderStatusRequestDate Date and time when the order status request was created.
-->
<!ATTLIST OrderStatusRequestHeader
    orderStatusRequestID    %string;        #REQUIRED
    orderStatusRequestDate  %datetime.tz;   #REQUIRED
>

<!-- OrderStatusRequestItem* Elements -->

<!--
	Represents an item on the Order Status Request. 
	
	ItemReference: Defines all references related to a line item.
	
	Comments: Contains any additional comments associated with this line item.

	Priority: Priority indicator, used to drive the priority of the orders for the suppliers.
		The Priority of the Order is to indicate the current Build priority associated to the PO line item. It 
		is not meant to update the priority in the PO.
-->
<!ELEMENT OrderStatusRequestItem (ItemReference, Comments?, Priority?)>
<!--
	@attr rescheduleRequest Reschedule request information is derived by MRP to suggest possible changes to 
		the Delivery Date. The proposed changes could be result in the status Reschedule In, Reschedule Out, 
		or Cancellation request for the Order.
		<ul>
		<li>in - MRP proposal to reschedule the Delivery date prior to the current delivery date.</li>
		<li>out - MRP proposal to reschedule the Delivery date post the current delivery date.</li>
		<li>cancel - MRP proposal to cancel the PO line item.</li>
		</ul>

	@attr rescheduleDate The MRP proposed rescheduled date is captured in the date field. The reschedule date 
		is with reference to the delivery date.
-->
<!ATTLIST OrderStatusRequestItem
    rescheduleRequest (in|out|cancel) #IMPLIED
    rescheduleDate    %datetime.tz;   #IMPLIED
>

<!--
    Defines all references related to an schedule line in the confirmation.
-->
<!ELEMENT ScheduleLineReference (Extrinsic*)>
<!--
    @attr quantity The quantity as informed in the ScheduleLine element of the OrderRequest document
		which the current document is referring to.

    @attr requestedDeliveryDate The requested delivery date as informed in the ScheduleLine element of the
		OrderRequest document which the current document is referring to. 
		
	@attr lineNumber The line number identifier as informed in the ScheduleLine element of the
      OrderRequest document which the current document is referring to.
-->
<!ATTLIST ScheduleLineReference 
    quantity               %r8;           #REQUIRED
    requestedDeliveryDate  %datetime.tz;  #REQUIRED
    lineNumber             %uint;         #IMPLIED
>

<!--
    Request to add confirmation information to the data known about an
    order at the receiving server.

    While multiple confirmations may be sent for one order, each
    confirmation must mention a line item once. And, a line item must not
    be mentioned in more than one confirmation. Multiple confirmations are
    allowed (and sensible) only when each has type="allDetail" or
    type="detail". Only one (original) confirmation per order of
    type="accept", type="except" or type="reject" is allowed. When a
    confirmation with one of these types arrives, the receiving system must
    discard any and all previous confirmations for the order.

    OrderStatusRequestReference or OrderStatusRequestIDInfo can be optionally added to
    explicitly reference to the Order Status Request that originated the confirmation.

    ConfirmationItem elements may appear in any order within the
    ConfirmationRequest. Of course, an easily-readable order such as by
    increasing lineNumber value is preferred. Again, no line item
    (Confirmation@lineNumber attribute value) may appear more than once
    within a ConfirmationRequest element.
-->
<!ELEMENT ConfirmationRequest
    (ConfirmationHeader, OrderReference, (OrderStatusRequestReference | OrderStatusRequestIDInfo)*, ConfirmationItem*)>

<!--
    Information about this confirmation that's common to all contained
    items.

    The contained DocumentReference element may appear in a document only
    when the operation is "update". In that case, the DocumentReference is
    required and it must reference the most recent ConfirmationRequest
    document for this particular confirmation (usually also indicated by a
    common confirmID). For example when a confirmation is created, updated
    and then updated again, the final document must contain a
    DocumentReference referring to the previous ConfirmationRequest with
    operation="update". That document must, in turn, refer to the original
    (operation="new") ConfirmationRequest document.

    Tax and Shipping amounts may be updated (included in the confirmation
    with new values) without corresponding line item information
    (additional information in the ConfirmationItem list). Total should
    not be included with a different value from the OrderRequest document
    unless a ConfirmationItem describes a new UnitPrice or quantity. It is
    not necessary to copy this information from the OrderRequest document:
    Total, Tax and Shipping should not be included if they match those
    amounts in the original order.

    Contact roles may usefully include: technicalSupport, customerService,
    sales, shipFrom (starting point for shipments related to this order),
    shipTo (echoing the ShipTo element from the OrderRequest document),
    payTo (where payment for this order should be sent), billTo (echoing
    the BillTo element from the OrderRequest document), buyerCorporate
    (details the supplier has about the buying organization) and
    supplierCorporate. It is not necessary to copy this information from
    the OrderRequest document. The Contact element should be used primarily
    to add information to that known about an order or to describe the
    sender of this particular confirmation.

    Elements in the Contact list may appear in any order. A contact role
    (Contact@role attribute value) must not appear more than once within a
    ConfirmationHeader element.

    Elements in the Hazard list may appear in any order. The same hazard
    should not be listed more than once in a ConfirmationHeader. Each
    hazard listed at this level (in a ConfirmationHeader element) should
    apply to the entire order or all items mentioned in this confirmation.
    A ConfirmationRequest which updates the status of a single line item
    should not include Hazard elements in the ConfirmationItem element.

    The Comments element list may contain additional information about the
    status of the overall order (or the portion described in this
    confirmation) such as payment terms, additional detail on shipping
    terms and clarifications of the status. For the last type of
    information, terms such as "backordered", "shipped" and "invalid" may
    be sensible. All such data must be intended for human use. The text
    in this list should describe this particular confirmation and not
    repeat Comments appearing in the OrderRequest.

    Elements in the Comments list may appear in any order. The xml:lang
    attribute may have the same value in multiple Comments elements in the
    list. The set of Comments with a particular xml:lang value should
    contain similar content to that for any other xml:lang value present in
    the list.

    On the other hand, the Extrinsic element list may be used to insert
    additional data about the order for application consumption. These
    elements may include pre-defined keywords and values affecting workflow
    in the receiving system.

    Elements in the Extrinsic list may appear in any order. An extrinsic
    type (Extrinsic@name attribute value) must not appear more than once
    within a ConfirmationHeader element. A type may not be mentioned both
    in this list and in a particular ConfirmationStatus element. (The
    ConfirmationHeader must not contain a default extrinsic value
    overridden at the lower level.)

    IdReference: Internal Reference Number/Supplier Reference Number can be one of the 
		following:
		<ul>
		<li>Internal supplier number - This is the most common scenario; it represents the document number 
			created by the internal supplier ERP system.</li>
		<li>Contract - The contract number can be used to identify that a specific outbound document is 
			related to a specific contract.</li>
		<li>Internal criteria - Sometimes the supplier can enter a customized value in the field Supplier 
			Reference Number. Example: It can be the name of the person in charge of the follow-up of the 
			transaction.</li>
		</ul>
-->
<!ELEMENT ConfirmationHeader (DocumentReference?, Total?, Shipping?,
                              Tax?, Contact*, Hazard*, Comments*,
                              IdReference*, Extrinsic*)>
<!--
    @attr confirmID An (optional) identifier for this particular confirmation assigned
        by the supplier. A user-visible and secondary (to the payloadID
        attribute of the cXML element) identifier for this document. This
        value does not vary as a particular confirmation is updated. That
        is, operation="update" documents describing the status of the same
        items in the same order would share a confirmID with the original
        operation="new" ConfirmationRequest.

        When the confirmID does not appear in an operation="new"
        ConfirmationRequest, it must not appear in a corresponding
        operation="update" document. Links between these documents must be
        deduced using the DocumentReference element contained in the
        update's ConfirmationHeader and the payloadID attribute of the
        original or previous update.
		
	@attr operation The (optional) operational mode of the confirmation document.
        Defaults to "new".

        An "update" confirmation allows a supplier to correct an error in a
        confirmation or to add additional information learned later. In
        either case, an "update" document must be complete: All data from
        the original confirmation or a previous update should be discarded
        by the recipient. A ConfirmationRequest document does not provide
        default data for a subsequent operation="update" document. (This
        applies in particular to Hazard and Extrinsic elements, which
        should appear at either the header or item levels but not both.)

        The "completeness" requirement allows the information contained in
        a ConfirmationRequest to expand. No restrictions are placed on
        including additional items that have not yet been referenced in a
        ConfirmationRequest in an operation="update" ConfirmationRequest.
        However, added items must not have been mentioned in another
        ConfirmationRequest. This protocol supports neither splitting 
		confirmations (sending an operation="update" ConfirmationRequest 
		document describing a subset of the items in an earlier version) 
		nor any kind of consolidations of confirmations.

        An operation="update" ConfirmationRequest must contain the same
        confirmID (if any) as the previous version of the confirmation.
        When included, this attribute provides an unambiguous and direct
        connection between all versions of the confirmation.

        An operation="update" ConfirmationRequest must also include a
        DocumentReference element in the ConfirmationHeader. (See above
        for more information on this element.)  This effectively sequences
        multiple versions of a confirmation and may provide the only link
        between those versions. (See above under confirmID for more on the
        implications of leaving out that attribute.)  Other confirmations
        discarded through consolidations as described above are not
        explicitly referenced by the new, larger ConfirmationRequest
        document.

        A confirmation may not be deleted; the protocol does not include an
        operation="delete" option for this request. Suppliers must replace
        incorrect or invalid confirmations with correct information. A
        type="unknown" ConfirmationStatus will reset such information to
        its original state. This covers the case of an error in accepting
        or rejecting an item that has not been researched.
		
    @attr type The type of confirmation. Possible values:
		<ul>
		<li>accept - The entire order is accepted as described in the referenced
            OrderRequest document. A document of this type may contain 
			ConfirmationItem elements. If ConfirmationItem elements are present 
			in the ConfirmationRequest document, they must contain only
            ConfirmationStatus elements of type="accept".</li>
		<li>allDetail - This confirmation updates only those line items described in
            the following ConfirmationItem elements. Line items not
            mentioned specifically in this document retain their current
            status. Unlike the "detail" type (below), a confirmation of
            this type includes all information known by the supplier,
            whether or not it differs from the data provided in the
            original OrderRequest document.

            This type is provided for compatibility with current EDI and
            order entry tools (which commonly send the buyer a snapshot of
            an order in the supplier's systems). Due to the reconciliation
            issues caused by confirmations of this type, it is recommended
            that use of this type be considered a "bridge" strategy
            suitable only for the short term.

            A document of this type must contain ConfirmationItem elements.
            The contained ConfirmationStatus elements must have types
            "allDetail", "reject" or "unknown". "accept" , "detail" 
            or "backordered". Other status types are not allowed because they 
            conflict with the requirements of this document type.</li>
		<li>detail - This confirmation updates only those line items described 
			in the following ConfirmationItem elements. Line items not
            mentioned specifically in this document retain their current
            states. A confirmation of this type should include only
            information which differs from that in the original
            OrderRequest document.

            Later ConfirmationRequest documents that restore information
            provided in the OrderRequest should not include the variations
            described in an earlier ConfirmationRequest. For example, the
            Tax element may appear in a ConfirmationStatus of one
            ConfirmationRequest and not in an update to that confirmation.
            This signifies that the original OrderRequest actually
            contained the correct charge.

            A document of this type must contain ConfirmationItem elements.
            The contained ConfirmationStatus elements may have any type
            except "allDetail".</li>
		<li>backordered - The entire order is backordered.

            A document of this type may contain ConfirmationItem elements.
            If ConfirmationItem elements are present in the
            ConfirmationRequest document, they must contain only
            ConfirmationStatus elements of type="backordered".</li>
		<li>except - The entire order is accepted as described in the referenced
            OrderRequest document with exceptions listed in the following
            ConfirmationItem elements. Line items not mentioned
            specifically in this document are accepted without change.

            A document of this type must contain ConfirmationItem elements.
            The contained ConfirmationStatus elements may have any type
            except "allDetail".</li>
		<li>reject - The entire order is rejected. No further (line item) detail 
			is provided beyond the reason described in the Comments element.

            A document of this type must not contain ConfirmationItem
            elements.</li>
		<li>requestToPay - This type of confirmation is for the supplier to request the
            initiation of payment transactions for either the whole
            order or some line items.

            If the request contains no ConfirmationItem elements, the intent
            will be to initiate payment transaction on the total amount of
            all line items in the order, except those being rejected.

            If the request contains ConfirmationItem elements, payment
            transaction will be initiated against the specified items and
            quantities.

            A ConfirmationItem in this type of request does not have to
            describe the complete line item. It should only contain
            "requestToPay" ConfirmationStatus elements for new payment
            transactions.</li>
		<li>replace - The entire order is replaced. This is used by supplier 
			to perform a wholesale replacement of the order. In the case
            that a changed order is sent from Buyer before this
            OrderConfirmation is received by Buyer, Supplier should
            accept the new changed order. ConfirmationItems in this
            type of request must contain ConfirmationStatus elements
            only of type "detail", and each ConfirmationStatus element
            must include an ItemIn element.</li>
		</ul>

    @attr noticeDate The date and time this confirmation was created.
	
    @attr invoiceID An optional supplier-generated identifier for an Invoice 
		associated with the items described in this confirmation. Identical to 
		the Invoice Number that appears at the top of a physical invoice.
		
    @attr incoTerms Optional shipping terms as defined by the International Chamber of
        Commerce. These terms inform the buyer which portion of the
        shipping charges are their responsibility. Allowed values include:
		<ul>
		<li>cfr - Cost and freight</li>
		<li>cif - Cost, insurance and freight</li>
		<li>cip - Carriage and insurance paid to</li>
		<li>cpt - Carriage paid to</li>
		<li>daf - Delivered at frontier</li>
		<li>dap - Delivered at Place</li>
		<li>ddp - Delivered duty paid</li>
		<li>ddu - Delivered duty unpaid</li>
		<li>dpu - Delivered at Place Unloaded</li>
		<li>deq - Delivered ex quay (duty paid)</li>
		<li>des - Delivered ex ship</li>
		<li>exw - Ex works</li>
		<li>fas - Free alongside ship</li>
		<li>fca - Free carrier</li>
		<li>fob - Free on board vessel</li>
		</ul>
		
	@attr version The version number for this confirmation. It should start with 1 and
        should be incremented by 1 for each subsequent version (2,3,4...).
-->
<!ATTLIST ConfirmationHeader
    confirmID            %string;                        #IMPLIED
    operation            (new | update)                  "new"
    type (accept | allDetail | detail | backordered | except | reject | requestToPay | replace)
                                                         #REQUIRED
    noticeDate           %datetime.tz;                   #REQUIRED
    invoiceID            %string;                        #IMPLIED
    incoTerms (cfr | cif | cip | cpt | daf | dap | ddp | ddu | dpu | deq | des | exw |
               fas | fca | fob)                          #IMPLIED
    version              %uint;                          #IMPLIED
>

<!--
    Information about a specific line item. Must completely describe the
    status of the referenced line item. For example, two
    ConfirmationRequest documents may be necessary to update the status of
    an entire order. However, a particular item must be mentioned in only
    one of these documents.

    A particular line item from an order must not be mentioned in more than
    one ConfirmationItem element.

    Any Contact elements provided at this level describe contacts specific
    to this item. The ConfirmationHeader description mentions roles
    appropriate at this level as well. A particular Contact role must not
    appear in both the ConfirmationItem and ConfirmationHeader elements.

    Elements in the Contact list may appear in any order. A contact role
    (Contact@role attribute value) must not appear more than once within a
    ConfirmationItem element.

    Elements in the Hazard list may appear in any order. The same hazard
    should not be listed more than once in a ConfirmationItem. Each hazard
    listed at this level (in a ConfirmationItem element) must apply to the
    this specific line item. A ConfirmationRequest which updates the
    status of a single line item should not include Hazard elements in the
    ConfirmationItem element.

    The list of ConfirmationStatus elements may appear in any order.
    However, the described quantities must sum to the quantity of the
    containing ConfirmationItem element (after any conversions required by
    use of different UnitOfMeasure scales at each level). This ensures the
    status of a particular line item is completely described in a
    ConfirmationRequest document.
-->
<!ELEMENT ConfirmationItem
    (UnitOfMeasure, Contact*, Hazard*, ConfirmationStatus+)>
<!--
    @attr quantity Number of this line item which was ordered. Expressed in the units
        given in the UnitOfMeasure element. Copied directly from the
        corresponding line item (ItemOut) in the document referenced in the
        OrderReference element.

    @attr lineNumber Position (counting from 1) of this item in an order. Copied
        directly from the corresponding line item (ItemOut) in the document
        referenced in the sibling OrderReference element.

    @attr parentLineNumber Line number of the parent line item in the order confirmation request.

	@attr itemType Specifies the type of item. Possible values:
		<ul>
		<li>composite - Identifies an item group.</li>
		<li>item - Identifies an independent line item.</li>
		<li>lean - Indicates that no child item is expected for the line.</li>
		</ul>

	@attr compositeItemType Specify type of parent line item (lot/box).
-->
<!ATTLIST ConfirmationItem
    quantity             %r8;                  #REQUIRED
    lineNumber           %uint;                #REQUIRED
    parentLineNumber     %uint;                #IMPLIED
    itemType             %lineItemType;        #IMPLIED
	compositeItemType		%compositeItemType;		#IMPLIED
>

<!--
    Confirmation status of a specific line item or portion thereof.
    Quantities at this level must sum to that in the containing
    ConfirmationItem. Documents should use a consistent UnitOfMeasure in
    ConfirmationItem and contained ConfirmationStatus elements. A
    substitution (replacement item) may use a different UnitOfMeasure in
    the ItemDetail contained within the ItemIn element.

    In the normal case (for simple acceptance or rejection of this item),
    the ConfirmationStatus element would contain only a UnitOfMeasure
    element and the quantity and type attributes. ItemIn appears in this
    element if and only if the supplier is recommending a substitution or
    the type is "allDetail". In that case, the quantity of the ItemIn
    element must match that of the containing ConfirmationStatus unless the
    UnitOfMeasure is different. ItemDetail must appear inside ItemIn
    whenever ItemIn appears in this context. That is, an ItemIn element in
    a ConfirmationStatus element must contain an ItemDetail element.

    UnitPrice, Tax and Shipping amounts may be updated (included in the
    ConfirmationStatus element with new values) without a complete part
    substitution. It is not necessary to copy this information from the
    OrderRequest document: UnitPrice, Tax and Shipping should not be
    included if they match that in the original ItemOut element. The
    addition of tax or shipping amounts not mentioned in the original order
    may appear when the type is "accept", "allDetail" or "detail". The
    "accept" type should be used if these additions are the only changes
    from the order. The "detail" type implies a substitution (presence of
    the ItemIn element), price change (presence of the UnitPrice element)
    or delayed shipment (presence of the deliveryDate attribute).
    Unfortunately, the "allDetail" type requires reconciliation software to
    determine what has changed since the original order.

    The Comments element list may contain additional information about the
    status of this portion of the item such clarifications of the type
    attribute. Terms such as "backordered", "shipped" and "invalid" may be
    sensible. All such data must be intended for human use.

    Elements in the Comments list may appear in any order. The xml:lang
    attribute may have the same value in multiple Comments elements in the
    list. The set of Comments with a particular xml:lang value should
    contain similar content to that for any other xml:lang value present in
    the list.

    On the other hand, the Extrinsic element list may be used to insert
    additional data about this particular item portion for application
    consumption. These elements may include pre-defined keywords and
    values affecting workflow in the receiving system.

    Elements in the Extrinsic list may appear in any order. An extrinsic
    type (Extrinsic@name attribute value) must not appear more than once
    within a ConfirmationStatus element. A type may not be mentioned both
    in this list and in the overall ConfirmationHeader element. (The
    ConfirmationHeader must not contain a default extrinsic value
    overridden at this lower level.)

    SupplierBatchID: SupplierBatch is the batch ID provided by the supplier at the line item quantity level
        to identify  the batch in which the item or the product was produced. The batch id is helpful in tracebility.
        Batch numbers (also called "lot number" or "variant") are used in several industries to uniquely identify 
		items produced together.

    ComponentConsumptionDetails: Contains detailed information about consumption of components in the manufacturing 
		of final product.
-->
<!ELEMENT ConfirmationStatus
    (UnitOfMeasure, (ItemIn | (UnitPrice?, Tax?, Shipping?)),
    SupplierBatchID?, ScheduleLineReference?, ComponentConsumptionDetails*, Comments*, Extrinsic*)>
<!--
    @attr quantity Number of this line item which has this status. Expressed in the
        units given in the UnitOfMeasure element.
		
    @attr type The status of this particular portion of the order:
		<ul>
		<li>accept - This portion of the line item is accepted as described in the
            referenced ItemOut element.

            This type is allowed in documents with overall
            (ConfirmationHeader) types "accept", "detail" or "except".</li>
		<li>allDetail - This portion of the line item is accepted as detailed in the
            contents of this ConfirmationStatus element. These contents
            completely describe what will be shipped. Unlike the "detail"
            type (below), a confirmation of this type includes all
            information known by the supplier, whether or not it differs
            from the data provided in the original OrderRequest document.

            This type is provided for compatibility with current EDI and
            order entry tools (which commonly send the buyer a snapshot of
            an order in the supplier's systems). Due to the reconciliation
            issues caused by confirmations of this type, it is recommended
            that use of this type be considered a "bridge" strategy
            suitable only for the short term.

            This type is allowed only in documents with overall
            (ConfirmationHeader) type "allDetail".</li>
		<li>detail - This portion of the line item is accepted with the changes
            detailed in the contents of this ConfirmationStatus element.
            At least one of the UnitPrice, Shipping, Tax or ItemIn elements
            or the deliveryDate attribute must be present.

            This type is allowed in documents with overall
            (ConfirmationHeader) types "detail", "except" or "replace".</li>
		<li>backordered - This portion of the line item is backordered.

            This type is allowed in documents with overall
            (ConfirmationHeader) types "backordered", "detail" ,"except",
            and "allDetail". "shipmentDate" and "deliveryDate" attributes for 
            this type of ConfirmationStatus are optional. The rest of elements
            for this type of ConfirmationStatus are ignored.</li>
		<li>reject - This portion of the line item is rejected.

            This type is allowed in documents with overall
            (ConfirmationHeader) types "allDetail", "detail" or "except".</li>
		<li>requestToPay - A requestToPay is being asked about this portion of the line
            item. Request-to-pay will initiate a request to the financial
            institution to begin the settlement process of the portion of
            the line item.

            This type is allowed in documents with overall request
            (ConfirmationHeader) type "requestToPay".</li>
		<li>unknown - The status of this portion of the line item is not known at
            the time of this confirmation. This line item status may
            provide a "placeholder" while the supplier does further
            research. Update confirmations may also reset the status of a
            line item portion to "unknown" when an earlier confirmation
            (incorrectly) accepted or rejected that portion.

            This type is allowed in documents with overall
            (ConfirmationHeader) types "allDetail", "detail" or "except".</li>
		</ul>
		
    @attr shipmentDate The date and time this shipment is expected to leave the supplier.
        A ConfirmationStatus element should include this information if the
        type is "accept", "allDetail" or "detail".
		
    @attr deliveryDate The date and time this shipment is expected to arrive. Should not
        be included if the value matches that of the requestedDeliveryDate
        attribute (if any) in the corresponding OrderRequest document.
        Otherwise, a ConfirmationStatus element should include this
        information if its type is "accept", "allDetail" or "detail".
-->
<!ATTLIST ConfirmationStatus
    quantity             %r8;                             #REQUIRED
    type (accept | allDetail | detail | backordered | reject | unknown | requestToPay)
                                                          #REQUIRED
    shipmentDate         %datetime.tz;                    #IMPLIED
    deliveryDate         %datetime.tz;                    #IMPLIED
>

<!-- ====
    ShipNotice* transaction forwards ship notice data in the Request. No
    specific Response document is required for this transaction. Servers
    must respond to a ShipNoticeRequest with a generic Response document.
==== -->

<!--
    Request to add shipment information to the data known about an order at
    the receiving server.

    Shipments with multiple responsible carriers may be described in one of
    two ways:
    1. A single carrier (or third-party logistics provider) creates a
       tracking identifier that may be used to retrieve information about
       the entire trip. Suppliers send such information in a single
       ShipControl element. Optionally, they may use Route elements to
       describe lower-level carriers.
    2. Separate tracking numbers are required for each segment. Suppliers
       send such information with one ShipControl element per segment.

    ShipControl elements must appear in the order the shipment will travel.
    The first such element must not have an explicit starting date (the
    ShipControl@startDate attribute must not be present) and that carrier's
    control must begin at the shipment's origination time
    (ShipNoticeHeader@shipmentDate attribute value). All later ShipControl
    elements must have increasing (later) starting dates
    (ShipControl@startDate attribute value).

    ShipNoticePortion elements may appear in any order. A particular order
    (ShipNoticePortion/OrderReference/DocumentReference@payloadID attribute
    value) must not appear more than once in a ShipNoticeRequest element.

    Note: Many elements and attributes in the ShipNoticeRequest and
    ShipNoticeHeader elements are optional only for the operation="delete"
    case. For other operations, one or more ShipControl and
    ShipNoticePortion elements must appear in a ShipNoticeRequest element.
-->
<!ELEMENT ShipNoticeRequest
    (ShipNoticeHeader, ShipControl*, ShipNoticePortion*)>

<!--
    Information about this ship notice that's common to all contained
    items.

    One or more ServiceLevel elements must appear in all ShipNoticeRequest
    documents except when operation="delete" is specified. Each
    ServiceLevel must contain a single string corresponding to the level of
    service (such as "overnight") provided by the carrier for this
    shipment. When multiple ServiceLevel elements appear, all must
    describe the same level of service in different languages or locales.
    (No two ServiceLevel elements may have the same xml:lang attribute.)
    Elements in such a list may appear in any order.

    The contained DocumentReference element may appear in a document only
    when the operation is "update" or "delete". In that case, the
    DocumentReference is required and it must reference the most recent
    ShipNoticeRequest document for this particular ship notice (also
    indicated by a common shipmentID). For example when a ship notice is
    created, updated and then updated again, the final document must
    contain a DocumentReference referring to the previous ShipNoticeRequest
    with operation="update". That document must, in turn, refer to the
    original (operation="new") ShipNoticeRequest document.

    Contact roles may usefully include: technicalSupport, customerService,
    sales, shipFrom (starting point for this shipment), shipTo (should echo
    the ShipTo element from the OrderRequest documents), buyerCorporate
    (details the supplier has about the buying organization) and
    supplierCorporate. It is not generally necessary to copy information
    from the various OrderRequest documents. The Contact element should be
    used primarily to add information to that known about an order.

    Elements in the Contact list may appear in any order. A contact role
    (Contact@role attribute value) must not appear more than once within a
    ShipNoticeHeader element.
	
	LegalEntity and OrganizationalUnit elements are intended to include
	the company code and purchasing organization respectively. Currently
	only used for Ship Notices coming from buyer to supplier (Component or
	Return Ship Notice).

    Elements in the Hazard list may appear in any order. The same hazard
    should not be listed more than once in a ShipNoticeHeader. Each hazard
    listed at this level (in a ShipNoticeHeader element) should apply to
    the entire shipment or all items contained in this shipment. A
    ShipNoticeRequest for a single line item should not include Hazard
    elements in the ShipNoticeItem element.

    The Comments element list may contain additional information about the
    shipment. In this context (the ShipNoticeHeader), that information
    must be common to all contained items and routes. All such data must
    be intended for human use. This information could also include a reason 
    for shipment to indicate rush orders for example. 

    The Comments element list may also contain structured comments with 
    type as "ReasonForShipment" to describe any specific reason for this shipment
    and with type as "TransitDirection" to specify transit direction.
    Reason for shipment is typically specified with replace/partial shipments to
    let the buyer know about the need of the shipment.
    
    Elements in the Comments list may appear in any order. The xml:lang
    attribute may have the same value in multiple Comments elements in the
    list. The set of Comments with a particular xml:lang value should
    contain similar content to that for any other xml:lang value present in
    the list.

    On the other hand, the Extrinsic element list may be used to insert
    additional data about the shipment for application consumption. These
    elements may include pre-defined keywords and values affecting workflow
    in the receiving system.

    Elements in the Extrinsic list may appear in any order. An extrinsic
    type (Extrinsic@name attribute value) must not appear more than once
    within a ShipNoticeHeader element. A type may not be mentioned both in
    this list and in a particular ShipControl or ShipNoticePortion element.
    (The ShipNoticeHeader must not contain a default extrinsic value
    overridden at either lower level.)
    
    ReferenceDocumentInfo: Additional document reference information.
-->
<!ELEMENT ShipNoticeHeader (ServiceLevel*, DocumentReference?, Contact*, LegalEntity?, OrganizationalUnit*,
                            Hazard*, Comments*, TermsOfDelivery?, TermsOfTransport*, Packaging?, Extrinsic*, IdReference*, ReferenceDocumentInfo*)>
<!--
    @attr shipmentID An identifier for this particular shipment assigned by the
        supplier. A user-visible and secondary (to the payloadID attribute
        of the cXML element) identifier for this document. This value does
        not vary as a particular ship notice is updated. That is,
        operation="update" or operation="delete" documents describing the
        same shipment must share a shipmentID with the original
        operation="new" ShipNoticeRequest.

    @attr operation This optional attribute specifies whether the ShipNoticeRequest 
		document is new (the default) or an update to a previous ship notice. Possible
		values:
		<ul>
		<li>new (default) - Creates a new ship notice. No previous ship notice has been 
			sent.</li>
		<li>update - Updates a previous ship notice request. Allows a supplier to correct 
			an error in a ship notice or to add additional information learned later. In 
			either case, an "update" document must be complete: all data from the original 
			should be discarded by the recipient. The shipmentID must match a previous 
			request's shipmentID.</li>
		<li>delete - Removes the changes described in the previous new or updated ShipNoticeRequest 
			from the state of the shipment. Only use when the supplier discards a planned shipment 
			or incorrectly sends a ShipNoticeRequest about an order that will not take place. The 
			shipmentID must match a previous request's shipmentID.</li>
		</ul>

        If the operation is not "new" (explicitly or by default), the
        ShipNoticeRequest must also include a DocumentReference element in
        the ShipNoticeHeader. This effectively sequences multiple versions of a ship
        notice.
		
    @attr noticeDate Specifies the date and time the ShipNoticeRequest document was created.
		
    @attr shipmentDate The date and time the shipment left the supplier. You must specify 
		this attribute in all ShipNoticeRequest documents except when the operation is "delete".
		
    @attr deliveryDate Specifies the date and time this shipment is expected to arrive. 
		While this value can default to the requestedDeliveryDate of a single order, that 
		attribute is optional in an OrderRequest document, and the ShipNoticeRequest can 
		refer to multiple OrderRequest documents. You must include this attribute in all 
		ShipNoticeRequest documents except when the operation is "delete".
		
    @attr shipmentType Use this attribute to specify if the type of ship notice was actual 
		or estimated. Specify "actual" to indicate an actual ship notice and "planned" 
		for an estimated ship notice.
		
		When you specify "actual", you must enter the actual shipping date. If you specify 
		"planned", you must enter the estimated shipping date.
		
    @attr fulfillmentType The type of fulfillment for which this shipment notice is 
		created. Possible values are "partial" if all the items are not being shipped and 
		"complete" if the whole order is being shipped.
		
    @attr requestedDeliveryDate Specifies the desired date of delivery. In many cases, 
		it can reflect the time (or time frame) when the buyer is able and willing to receive 
		the goods.
		
    @attr reason The reason for the ship notice. The only possible value is "return", which 
		means the shipment contains items that are being returned from the buyer to the supplier 
		for some reason. For example, the items could be damaged or defective.

    @attr activityStepType An identifier for a business document. When creating a ship notice for a stock 
		transport order, you must specify one of the following values:: 
        <ul>
            <li>stockTransfer - Identifies a ship notice message sent from buyer to supplier for stock transfer
					scenario.</li>
            <li>stockShippingAdvice - Identifies a ship notice message sent from a supplier for stock transfer
					scenario.</li>
	    </ul>
-->
<!ATTLIST ShipNoticeHeader
    shipmentID           %string;              #REQUIRED
    operation          (new | update | delete) "new"
    noticeDate           %datetime.tz;         #REQUIRED
    shipmentDate         %datetime.tz;         #IMPLIED
    deliveryDate         %datetime.tz;         #IMPLIED
    shipmentType       (actual | planned)      #IMPLIED
    fulfillmentType    (partial | complete)    #IMPLIED
    requestedDeliveryDate  %datetime.tz;       #IMPLIED
    reason               (return)              #IMPLIED
    activityStepType   (stockTransfer | stockShippingAdvice) #IMPLIED
>

<!--
    One language-specific string for the service level code. Each
    ServiceLevel must contain a string (in the specified language)
    corresponding to the level of service (such as "overnight") provided by
    the carrier for this shipment.
-->
<!ELEMENT ServiceLevel (#PCDATA)>              <!-- string -->
<!--
    @attr xml:lang The language or locale in which the ServiceLevel content (the name
        of the service level) is written.
-->
<!ATTLIST ServiceLevel
   xml:lang              %xmlLangCode;         #REQUIRED
>

<!--
    Carrier responsible for some portion of the shipment. The shipment
    will be tracked using the identifiers provided at this level. Those
    identifiers should be valid from the startDate of one ShipControl
    element or the shipment's shipmentDate until the startDate of the next.

    The CarrierIdentifier list may include multiple identifiers for the
    same carrier. Elements in this list may appear in any order. A
    particular identification domain (CarrierIdentifier&#064;domain attribute
    value) must not appear more than once in a ShipControl element. The
    identification provided by all elements of the CarrierIdentifier list
    must correspond to the same company.

    If present, Route elements must be in the order the shipment will
    travel.

    The only Contact roles likely to be sensible in this element are
    "carrierCorporate" (details the contact information the supplier has
    about the carrier organization) and "shipFrom". A Contact element with
    role "shipFrom" must appear in all ShipControl elements after the
    first. This role must not appear in the first ShipControl element
    since it would duplicate that role in the overall ShipNoticeHeader
    element. Since a Contact with role "shipTo" would always duplicate
    information in the following ShipControl element or that role in the
    ShipNoticeHeader, such a Contact must not appear in this element. In
    essence, control passes from one carrier to another at a particular
    location and (estimated) time.

    Elements in the Contact list may appear in any order. A contact role
    (Contact@role attribute value) must not appear more than once within a
    ShipControl element.

    The Comments element list may contain additional information about the
    shipment while under the control of this carrier. In this context (the
    ShipControl element), that information must be common to all contained
    routes or make it clear which Route is affected. All such data must be
    intended for human use.

    Elements in the Comments list may appear in any order. The xml:lang
    attribute may have the same value in multiple Comments elements in the
    list. The set of Comments with a particular xml:lang value should
    contain similar content to that for any other xml:lang value present in
    the list.

    On the other hand, the Extrinsic element list may be used to insert
    additional data about this carrier or their period of responsibility
    for application consumption. These elements may include pre-defined
    keywords and values affecting workflow in the receiving system.

    Elements in the Extrinsic list may appear in any order. An extrinsic
    type (Extrinsic@name attribute value) must not appear more than once
    within a ShipControl element. A type may not be mentioned both in this
    list and in the overall ShipNoticeHeader element. (The
    ShipNoticeHeader must not contain a default extrinsic value overridden
    at this lower level.)
-->
<!ELEMENT ShipControl (CarrierIdentifier+, ShipmentIdentifier+,
                       PackageIdentification?, (Route|TransportInformation)*, Contact*, Comments*,
                       Extrinsic*)>
<!--
    @attr startDate The date and time this shipment started this part of the route.
        Required for all ShipControl elements after the first. This
        attribute must not appear in the first ShipControl element since it
        would duplicate the ShipNoticeHeader's shipmentDate attribute.
-->
<!ATTLIST ShipControl
    startDate            %datetime.tz;         #IMPLIED
>

<!--
    Identifiers which appear on the containers, skids, boxes or packages
    that constitute the shipment. The range of numbers described is
    inclusive at both extremes.
-->
<!ELEMENT PackageIdentification EMPTY>
<!--
    @attr rangeBegin Earliest number appearing on the separate elements in this
        shipment.
		
    @attr rangeEnd Highest number appearing on the separate elements in this
        shipment. Must be greater than or equal to rangeBegin.
-->
<!ATTLIST PackageIdentification
    rangeBegin           %uint;                #REQUIRED
    rangeEnd             %uint;                #REQUIRED
>

<!--
    Purchase order and item data. Answers the question: What's coming in
    this shipment?

    A particular OrderRequest (via the OrderReference element) must not be
    mentioned in more than one ShipNoticePortion element. While multiple
    shipments may be sent for one order, a ship notice must mention each
    order only once.

    MasterAgreementReference or MasterAgreementIDInfo can be used to identify 
    the corresponding scheduling agreement if the ship notice is generated
    from a scheduling agreement release.

    Any Contact elements provided at this level describe contacts specific
    to this order. The ShipNoticeHeader description mentions roles
    appropriate at this level as well, though shipFrom, shipTo,
    buyerCorporate and supplierCorporate information should not vary at
    this level. A particular Contact role must not appear in both the
    ShipNoticePortion and ShipNoticeHeader elements. Therefore, roles such
    as technicalSupport, customerService and sales are most appropriate
    within the ShipNoticePortion.

    Elements in the Contact list may appear in any order. A contact role
    (Contact@role attribute value) must not appear more than once within a
    ShipNoticePortion element.

    The Comments element list may contain additional information about the
    order in this shipment. In this context (the ShipNoticePortion
    element), that information must be common to all contained items or
    make it clear which ShipNoticeItem is affected. All such data must be
    intended for human use.

    Elements in the Comments list may appear in any order. The xml:lang
    attribute may have the same value in multiple Comments elements in the
    list. The set of Comments with a particular xml:lang value should
    contain similar content to that for any other xml:lang value present in
    the list.

    On the other hand, the Extrinsic element list may be used to insert
    additional data about this order for application consumption. These
    elements may include pre-defined keywords and values affecting workflow
    in the receiving system.

    Elements in the Extrinsic list may appear in any order. An extrinsic
    type (Extrinsic@name attribute value) must not appear more than once
    within a ShipNoticePortion element. A type may not be mentioned both
    in this list and in the overall ShipNoticeHeader element. (The
    ShipNoticeHeader must not contain a default extrinsic value overridden
    at this lower level.)

    If a ShipNoticePortion element contains no ShipNoticeItem elements, the
    entire referenced order is included in the shipment. 
	
	Note: This simplifying option prevents inclusion of hazard and packaging
    information.
    
    ReferenceDocumentInfo: Additional document reference information.
-->
<!ELEMENT ShipNoticePortion
    (OrderReference, (MasterAgreementReference | MasterAgreementIDInfo)?,  Contact*, Comments*, Extrinsic*, ShipNoticeItem*, ReferenceDocumentInfo*)>

<!--
    Portion of a specific line item that is part of this shipment.

    A line item from an order must not be mentioned in more than one
    ShipNoticeItem element. While additional quantities of an item may be
    shipped separately, a single ship notice must describe each item only
    once.

    Elements in the Hazard list may appear in any order. The same hazard
    should not be listed more than once in a ShipNoticeItem. Each hazard
    listed at this level (in a ShipNoticeItem element) must apply to this
    specific line item. A ShipNoticeRequest for a single line item should
    not include Hazard elements in the ShipNoticeItem element.
    
    ShipNoticeItemDetail contains detailed information about an item. All the data that
    a user would want to see about an item instead of the bare essentials.
    In a ship notice the item details are inherited from the purchase order 
    that is referenced. For ship notices without order reference the item details 
    can be retrieved from this section.

    Batch: An element carrying batch information for material or goods produced
		in a single manufacturing run.
		
		There could be a situation that for each ASN line item there could be multiple 
		batches which could be produced on same day and will have same expiry date.
		Each batch can have specific quantities, the sum of these quantity will be equal to 
		ASN line item quantity. 

    SupplierBatchID: SupplierBatch is the batch ID provided by the supplier at the line item quantity 
		level to identify  the batch in which the item or the product was produced. The batch id is 
		helpful in traceability. Batch numbers (also call "lot number", "variant") are used in several 
		industries to uniquely identify items produced together.
		
	AssetInfo: Provides asset tag numbers or serial numbers for individual items in a shipment of goods.
	
	TermsOfDelivery: Specifies terms of delivery at the line-item level.
		
	ShipNoticeItemIndustry: Contains a grouping of industry-specific fields.

    ComponentConsumptionDetails: Contains detailed information about consumption of components in the 
		manufacturing of final product.
        
    ReferenceDocumentInfo: Additional document reference information.

    Comments: This element contains the ship notice item specific comments. Could be used to attach 
		quality certificates that were required in OrderRequest. In this case, one comment will be 
		provided by attachment, the certificate type will be provided in Comments@type attribute.
		
	Extrinsic: Contains any additional information for this ship notice line item.

	IUIDInfo: Provides item unique identification information.
-->
<!ELEMENT ShipNoticeItem (ItemID?, ShipNoticeItemDetail?,  UnitOfMeasure, Packaging*, Hazard*, (Batch | SupplierBatchID)*,
				AssetInfo*, TermsOfDelivery?, OrderedQuantity?, ShipNoticeItemIndustry?, ComponentConsumptionDetails*, ReferenceDocumentInfo*, Comments*, Extrinsic*, IUIDInfo?)>
<!--
    @attr quantity Number of this line item which was shipped. Expressed in the units
        given in the UnitOfMeasure element.

    @attr lineNumber Position, counting from 1, of the item in an order. Matches the corresponding line item, 
		ItemOut, in the document referenced by the OrderReference element.    

    @attr parentLineNumber The line number of the corresponding parent line item. This attribute is applicable 
		only for a line item with itemType="item".

    @attr shipNoticeLineNumber Ship notice line number related to this item. Used when there are multiple ship 
		notice line items for a single purchase order line items.

	@attr itemType Specifies the type of item. Possible values:
		<ul>
		<li>composite - Identifies an item group.</li>
		<li>item - Identifies an independent line item.</li>
		<li>lean - Indicates that no child item is expected for the line.</li>
		</ul>

	@attr compositeItemType Specifies whether a parent item uses group-level pricing.
	
	@attr stockTransferType specifies the type of stock transport order. Possible values:
			<ul>
			<li>intra - Identifies the ship notice item as created for intra stock transport order.</li>
			<li>inter - Identifies the ship notice item as created for inter stock transport order.</li>
			</ul>
			
	@attr outboundType specifies the value for which outbound delivery needs to be created. Possible values:
            <ul>
            <li>stockTransport - Identifies the outbound delivery needs to be created for stock transport order.</li>
            </ul>
-->
<!ATTLIST ShipNoticeItem
    quantity             %r8;                  #REQUIRED
    lineNumber           %uint;                #REQUIRED
    parentLineNumber     %uint;                #IMPLIED
    shipNoticeLineNumber     %uint;            #IMPLIED
    itemType             %lineItemType;        #IMPLIED
	compositeItemType	 %compositeItemType;   #IMPLIED
	stockTransferType    (intra | inter)       #IMPLIED
	outboundType         (stockTransport)      #IMPLIED 
	
>


<!--
    ShipNoticeItemDetail contains detailed information about an item. All the data that
    a user would want to see about an item instead of the bare essentials
    that are represented in the ItemID.
  
	In a ship notice the item details are inherited from the purchase order that is referenced. 
	For ship notices without order reference the item details can be retrieved from this section.

	UnitPrice: Price per unit of item. Optional Modifications 
		(changes due to discounts or fixed amount) may be provided.

	Description: Description is a string which describes something.

	UnitOfMeasure: Must be a UN/CEFACT (Recommendation 20) unit of measure code.

	PriceBasisQuantity: Define the quantity as quoted in the UnitPrice

	Classification: Classification is used to group items into similar categories.

	ManufacturerPartID: ID with which the item's manufacturer identifies the item.

	ManufacturerName: Name of the item's manufacturer.

	Dimension: Item dimensions.
    
	ItemDetailIndustry: Industry specfic item detail information.
	
	Extrinsic: Contains any additional information related to this item.

	ImportInfo: Provides import-related information for the shipment.
-->
<!ELEMENT ShipNoticeItemDetail (UnitPrice?, Description*, UnitOfMeasure?,     PriceBasisQuantity?, Classification*, ManufacturerPartID?,
                      ManufacturerName?, Dimension*, ItemDetailIndustry?, Extrinsic*, ImportInfo?)>


<!--
    Defines the header information of a ServiceEntryRequest.

    PartnerContact: All parties involved in this service entry sheet, including fieldContractor,
        fieldEngineer and requester.
    
	DocumentReference: The DocumentReference is optional and, if it is provided, it must reference 
		the ServiceEntryRequest which is related to the current ServiceEntryRequest.
	
	DocumentInfo: ID of a document entry known to the system. This attribute is used to reference 
		a document when DocumentReference is omitted. If both DocumentReference and 
		DocumentInfo are provided, they must refer to the same ServiceEntryRequest.

    ServiceEntryDetailLineIndicator: Header level indicators indicating certain information is
        provided at SES line level (in ServiceEntryItem).
	
    ServiceEntryDetailShipping: The shipping details related with SES(auto-generate invoice). It will be ignored if
        isShippingInLine is true.
	
	ShipNoticeIDInfo: References shipment related document identifiers.
	
	PaymentTerm: This defines the payment terms for this invoice.
	
    Period: The total time period during which all the services specified in this service
        entry sheet were rendered. Period at the header level does not imply anything
        about the Period at the line level.

    Comments: Comment text for this service entry sheet. Could be used to attach quality certificates that 
		were required in a service PO. In this case, one comment will be provided by attachment, 
        the certificate type will be provided in Comments@type attribute.

    IdReference
        Internal Reference Number/Supplier Reference Number can be one of the following:
			<ul>
			<li>Internal supplier number - This is the most common scenario; it represents the 
				document number created by the internal supplier ERP system.</li>
			<li>Contract - The contract number can be used to identify that a specific outbound 
				document is related to a specific contract.</li>
			<li>Internal criteria - Sometimes the supplier can enter a customized value in the 
				field Supplier Reference Number. Example: It can be the name of the person in 
				charge of the follow-up of the transaction.</li>
			</ul>

    Extrinsic: Additional information related to this service entry sheet. Extrinsic should not
        duplicate any information in ServiceEntryRequestHeader or ServiceEntryRequest.
-->
<!ELEMENT ServiceEntryRequestHeader (PartnerContact*,
                                     (DocumentReference | DocumentInfo)?,
                                     ServiceEntryDetailLineIndicator?,
                                     ServiceEntryDetailShipping?,
                                     ShipNoticeIDInfo?,
                                     PaymentTerm*,
                                     Period?,
                                     Comments*,
                                     IdReference*,
                                     Extrinsic*)>
<!--
    @attr serviceEntryDate The date and time when this service entry sheet was created, which should be
        earlier than the document's timestamp.

    @attr serviceEntryID A supplier-generated identifier for this service entry sheet, identical to the
        SES Number which appears at the top of a physical service entry sheet.

    @attr supplierReferenceNumber An internal reference to the supplier of this service entry sheet..

	@attr operation How this document is acting on the service entry sheet:
		<ul>
		<li>new (default) - This document creates a new service entry sheet.</li>
        <li>update - This document updates an existing service entry sheet. That existing
            service entry sheet must be specified as DocumentReference or DocumentInfo.</li>
		<li>delete - This document cancels an existing service entry sheet. That existing
			service entry sheet must be specified as DocumentReference.</li>
		</ul>
	
	@attr isFinal Set to "yes" to indicate that this item is closed, and no more deliveries are expected 
		for it.
-->
<!ATTLIST ServiceEntryRequestHeader
    serviceEntryDate   %datetime.tz;        #REQUIRED
    serviceEntryID     %string;             #REQUIRED
    supplierReferenceNumber     %string; 	#IMPLIED
    operation     (new | update | delete)   "new"
	isFinal       (yes)                     #IMPLIED
>


<!--
    Defines information related to a purchase order.

    The various content options for ServiceEntryOrderInfo are in order
    of preference. For example, OrderReference is strongly recommended.
    If OrderReference is not available, OrderIDInfo should be used in that order.

    OrderReference: The reference to the purchase order for the service.

    OrderIDInfo: The buyer system order ID for the purchase order for the service.
-->
<!ELEMENT ServiceEntryOrderInfo
    (OrderReference | OrderIDInfo)>



<!--
    Defines all references related to a line item in this service entry sheet.

    ItemID: The service part number.

    IdReference: Unique reference to part number.

    Classification: The classification code for of the service.

    Description: Textual description of the item.
-->
<!ELEMENT ItemReference (ItemID, IdReference*,Classification*, Description?)>
<!--
    @attr lineNumber The line number of the current line item referred to in OrderRequest.
        For planned item, this refers to the corresponding item in purchase order.
        For unplanned item, this refers to the parent item in purchase order.
-->
<!ATTLIST ItemReference
	lineNumber %uint; #REQUIRED
>


<!--
    Defines a line item in the ServiceEntryRequest.

    UnitRate: The rate at which the service item is charged.
        Use of the UnitRate element is recommended over the plain (UnitOfMeasure, UnitPrice)
        pair, since the UnitRate element includes the rate code. For some services such as
        temporary labor services, UnitRate is required.

    UnitOfMeasure: The unit of measure.

    UnitPrice: The unit price.
     
	PriceBasisQuantity: The price basis quantity. 		
	 
    ItemReference: The references related to this line item.

    MasterAgreementReference: The reference to the corresponding contract master agreement if
        the order for the service is a release purchase order.

    MasterAgreementIDInfo: The buyer's system ID for the corresponding contract master agreement if
        the order for the service is a release purchase order.

    Period: The time period during which the service was rendered.

    SubtotalAmount: The subtotal of the current item = unit price x serviced quantity.
	
	Tax: The tax info for this line item. It will be ignored if isTaxInLine
        is false.

	GrossAmount: The SubtotalAmount plus taxes, shipping, and special handling
        charges.
	
	ServiceEntryDetailLineSpecialHandling: The special handling info for this line item. 
		It will be ignored if isSpecialHandlingInLine is false.

    ServiceEntryDetailLineShipping: The shipping info for this line item. It will be ignored if
        isShippingInLine is false.

    ShipNoticeIDInfo: References shipment related document identifiers.
	
	ServiceEntryDetailDiscount: The discount info for this line item. It will be ignored if
        isDiscountInLine is false.
	
	ServiceEntryItemModifications: The additional Charges, Allowances and their Taxes that are 
        incurred for the total landed cost of the goods and service 
        contained in this SES item.
	
	TotalCharges: The total sum of all the charges applied on the goods and 
        services at the line item level in an SES.

    TotalAllowances: The total sum of all the allowances applied on the goods and
        services at the line item level in an SES.

    TotalAmountWithoutTax: The total sum of the SubTotal, Charges (including special handling
        charges and shipping charges), Allowances (including discounts) 
        applied at the line item level in an SES. This does not include taxes.
    
    NetAmount: The GrossAmount minus discount amount.
	
    Distribution: Accounting distribution information, including cost center, GL#, etc.

    Comments: Comment text for this line item.

    Extrinsic: Additional information related to this line item. Extrinsic should not
        duplicate anything in ServiceEntryItem or ServiceEntryOrder.
-->
<!ELEMENT ServiceEntryItem ( ItemReference,
                            (MasterAgreementReference | MasterAgreementIDInfo)?,
				            (UnitRate | (UnitOfMeasure, UnitPrice, PriceBasisQuantity?))?,
                            Period?, SubtotalAmount?,
                            Tax?, GrossAmount?,
                            ServiceEntryDetailLineSpecialHandling?,
                            ServiceEntryDetailLineShipping?, ShipNoticeIDInfo?,
                            ServiceEntryDetailDiscount?, ServiceEntryItemModifications?, 
                            TotalCharges?, TotalAllowances?, TotalAmountWithoutTax?, NetAmount?,
                            Distribution*, Comments*, Extrinsic*)>
<!--
    @attr serviceLineNumber The supplier-defined ID for the current service line, which should be
        unique for each service line in the same ServiceEntryRequest.

    @attr quantity The quantity serviced for the current line item.

	@attr referenceDate The reference date for the blanket order or contract item. The usage of 
		this attribute is optional in most cases and must be defined by the trading partners involved 
		in the transaction. Procurement software might use this date in reconciling an invoice against 
		a blanket order or contract.
	
	@attr inspectionDate The date when the transfer of goods or the delivery of services occurs 
		according to legal tax definitions. The usage of this attribute is optional in most cases, 
		and must be defined by the trading partners involved in the transaction.
		
    @attr type Specifies the type of ServiceEntryItem. Possible values:
		<ul>
		<li>material - Material goods delivered as part of a service order.</li>
		<li>service - Services delivered as part of a service order.</li>
		</ul>

    @attr isAdHoc Indicates the item does not exist in the reference document or contract master agreement.
-->
<!ATTLIST ServiceEntryItem
    serviceLineNumber        %uint;         #REQUIRED
    quantity                 %r8;           #REQUIRED
    referenceDate            %datetime.tz;  #IMPLIED
    inspectionDate           %datetime.tz;  #IMPLIED
    type                 %serviceItemType;  #IMPLIED
    isAdHoc                (yes)          #IMPLIED 
>


<!--
    Defines the service information for a purchase order or contract master agreement
    with item details.

    In this case, a line in a service entry sheet is a ServiceEntryItem and its
    line number is ServiceEntryItem@serviceLineNumber.

    ServiceEntryOrderInfo: The reference information for the purchase order or contract master agreement.

    ServiceEntryItem: The information for an item in the purchase order.
-->
<!ELEMENT ServiceEntryOrder (ServiceEntryOrderInfo, ServiceEntryItem+)>


<!--
    Defines the summary information for a ServiceEntryRequest.

    SubtotalAmount: The sum of amounts for all quantities.
		
	Tax: Tax info.
	
	SpecialHandlingAmount: Special handling charge.

    ShippingAmount: Shipping charge.
		
	GrossAmount: Sum of subtotal, taxes, special handling
        charges, and shipping charges, before discounts.
	
	ServiceEntryDetailDiscount: The total discount applied in this ServiceEntryRequest.
        Its contained percentageRate attribute will be ignored if isDiscountInLine is true.
		
	ServiceEntryHeaderModifications: The additional Charges, Allowances and their Taxes that are incurred
        for the total landed cost of the goods and services within SES header. This value specified at the 
		header-level is not the aggregated value towards the charges or allowances available for the line items.
	
	ServiceEntrySummaryLineItemModifications: Summary of all modifications applied on the goods and services at 
		the line item level in an service entry.
		
	TotalCharges: The total sum of all the charges applied on the goods and services at 
        the header-level and line item level in SES.

    TotalAllowances: The total sum of all the allowances applied on the goods and services
        at the header-level and  line item level in SES.

    TotalAmountWithoutTax: The total sum of the SubTotal, Charges (including special handling charges
        and shipping charges), Allowances (including discounts) applied at the header-level and  line item 
		level in SES. This does not include taxes.
	
	NetAmount: Gross amount minus discounts.
		
	DepositAmount: Total deposit/prepayment amount.

    DueAmount: Total amount due and payable. It equals NetAmount minus DepositAmount. If ServiceEntryRequest@purpose is
        "creditMemo" or "lineLevelCreditMemo", this amount must be negative. If ServiceEntryRequest@purpose is
        "debitMemo", this amount must be positive.

-->
<!ELEMENT ServiceEntrySummary (SubtotalAmount, Tax?, SpecialHandlingAmount?,
                               ShippingAmount?, GrossAmount?,
                               ServiceEntryDetailDiscount?, ServiceEntryHeaderModifications?, ServiceEntrySummaryLineItemModifications?,
                               TotalCharges?, TotalAllowances?, TotalAmountWithoutTax?, NetAmount?,
                               DepositAmount?, DueAmount?)>


<!--
    Transmits a service entry sheet request to a buyer, providing purchase order
    references, line item references, partners involved, and so on.

    Suppliers can use a ServiceEntryRequest to send a service entry sheet for
    any or all selected line items from a purchase order.

    This document must be sent to the location specified in the buyer's
    ProfileResponse.

    ServiceEntryRequestHeader: The header information for this service entry sheet.

    ServiceEntryOrder: The service entry sheet information for a purchase order with item details.

    ServiceEntrySummary: The summary information for this service entry sheet.
-->
<!ELEMENT ServiceEntryRequest (ServiceEntryRequestHeader, ServiceEntryOrder, ServiceEntrySummary)>

<!--
    Contains retail-specific elements.
	
    BestBeforeDate: The best-before-date helps with in FDA-related deliveries for go/no-go criteria. This is a common 
		element and hence can be found in the common proposal.

    ExpiryDate: ExpiryDate is used mainly in retail industries for FDA-related deliveries for go/no-go criteria.

	FreeGoodsQuantity: Quantities delivered but not charged for; those can be samples or promotions or just "fill up" 
		articles; has financial impact because those will not be on the commercial invoice (or marked with value 0.00)
	  
    PromotionDealID: Number assigned by a supplier to a special promotion activity. Promotions affect the forward 
		planning/ordering process (and the related pricing). The definition can be found in the common proposal.
	  
    PromotionVariantID: Promotional variance info associated with the ship notice item.
-->
<!ELEMENT ShipNoticeItemRetail (BestBeforeDate?, ExpiryDate?, FreeGoodsQuantity?,  PromotionDealID?, PromotionVariantID?)>

<!--
    Contains industry-specific elements.
	
	ShipNoticeItemRetail: Contains retail-specific field details that can be grouped together for ship notices.
	
	ShipNoticeItemLifeSciences: Contains life sciences specific elements for ship notices.
-->
<!ELEMENT ShipNoticeItemIndustry (ShipNoticeItemRetail?, ShipNoticeItemLifeSciences?)>

<!--
    Defines discount applied to the service item.

    Money: The total discount in dollar (or other currency) amount.
-->
<!ELEMENT ServiceEntryDetailDiscount (Money)>
<!--
    @attr percentageRate The discount rate in percentage.
-->
<!ATTLIST ServiceEntryDetailDiscount
    percentageRate          %r8;           #IMPLIED
>

<!--
    ServiceEntryItemModifications represents the additional Charges and Allowances
    that are incurred for the total landed cost of the goods and service 
    contained in this SES item.
 
    Modification: Provides the details of a Charge or an Allowance.
-->
<!ELEMENT ServiceEntryItemModifications (Modification+)>

<!--
    Defines the shipping information related to SES (auto-generated invoice).

    Contact: The ship from and ship to addresses. Both ship from and ship to
        must be specified. Allowed contact roles are shipFrom, shipTo and
        carrierCorporate.

    CarrierIdentifier: This list may include multiple identifiers for the same carrier.
        Elements in this list may appear in any order. A particular
        identification domain (CarrierIdentifier&#064;domain attribute
        value) must not appear more than once in an ServiceEntryDetailShipping
        element. The identification provided by all elements of the
        CarrierIdentifier list must correspond to the same company.

    ShipmentIdentifier: The tracking number of this shipment or a bill of lading number,
        depending on the domain attribute.

    DocumentReference: The reference to an earlier ShipNoticeRequest.
-->
<!ELEMENT ServiceEntryDetailShipping (Contact, Contact+,
                                 (CarrierIdentifier+, ShipmentIdentifier+)?,
                                 DocumentReference?)>

<!--
    ServiceEntryHeaderModifications represents the additional Charges, Allowances 
    that are incurred for the total landed cost of the goods and services within 
    the SES header. This value specified at the header-level is not the aggregated 
    value towards the charges or allowances available for the line items.

    Modification: Provides the details of a Charge or an Allowance.
-->
<!ELEMENT ServiceEntryHeaderModifications (Modification+)>

<!--
    Defines SES line level indicators indicating certain
    information is provided at SES line level (in ServiceEntryDetailItem).
    By default, all indicators are false.

    If an indicator indicates certain information (for example, tax, shipping, 
	and so on.) will be provided at SES line level, for SES lines which do
    not provide such information, they will be assumed to have values of
    zero, or "not available", whichever is appropriate, for that information.
-->
<!ELEMENT ServiceEntryDetailLineIndicator EMPTY>
<!--
    @attr isTaxInLine True if tax info (Tax) is provided at SES line level.

    @attr isSpecialHandlingInLine True if special handling info is provided at SES line level.

    @attr isShippingInLine True if shipping info is provided at SES line level.

    @attr isDiscountInLine True if discount info (ServiceEntryDetailDiscount) is provided
        at SES line level.

    @attr isAccountingInLine True if accounting distribution info (Distribution) is provided at
        SES line level.
-->
<!ATTLIST ServiceEntryDetailLineIndicator
    isTaxInLine                            (yes)      #IMPLIED
    isSpecialHandlingInLine                (yes)      #IMPLIED
    isShippingInLine                       (yes)      #IMPLIED
    isDiscountInLine                       (yes)      #IMPLIED
    isAccountingInLine                     (yes)      #IMPLIED
>

<!--
    Defines special handling information of the current SES line.

    Description: Textual description of the special handling.

    Money: The special handling amount.
-->
<!ELEMENT ServiceEntryDetailLineSpecialHandling (Description?, Money)>

<!--
    Defines shipping information of the current SES line.

    ServiceEntryDetailShipping: The shipping details.

    Money: The shipping amount.
-->
<!ELEMENT ServiceEntryDetailLineShipping (ServiceEntryDetailShipping, Money)>

<!--
    Defines information related to a summary of line modifications (allowances/charges) in Service Entry Sheet.

    Modification: Summary of one modification.
-->	
<!ELEMENT ServiceEntrySummaryLineItemModifications (Modification+)>

<!--
    Contains information relevant to the life sciences industry.
    One of the elements StudyID, ProtocolID or PoolID needs to be sent with a value otherwise the message will be rejected. 
    Also, if the value of the attribute isCFGPooled has a value ‘yes’ on the corresponding OrderRequest message then the poolID value 
    should also be provided in the message.    
    Study: Contains information about a clinical study. 
    ProtocolD: Contains a unique external identifier assigned to the protocol. The protocol is the detailed plan of the clinical study.
	MedicationListInfo: Medication list info is a set of medication sequence numbers.
	Extrinsic: Contains any additional information related to the ShipNoticeItemLifeSciences element.
-->
<!ELEMENT ShipNoticeItemLifeSciences (Study?, ProtocolID?, PoolID?,  MedicationListInfo*, Extrinsic*)>

