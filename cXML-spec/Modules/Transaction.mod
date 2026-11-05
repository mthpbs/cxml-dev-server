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
	Detailed per-unit asset information for a line item.
	 
    Extrinsic: Additional per-unit asset information.
-->
<!ELEMENT AssetInfo (Extrinsic*)>
<!--
	@attr isReferencedAsset Indicates whether the asset info is for a referenced asset associated with the item in the document.
	
	@attr tagNumber Asset tag number for the unit.

	@attr serialNumber Manufacturer's serial number for the unit.

	@attr location Location of the unit.
	
	@attr equipmentId Equipment ID of the unit.
-->
<!ATTLIST AssetInfo
    isReferencedAsset  (no)  #IMPLIED
	tagNumber %string; #IMPLIED
	serialNumber %string; #IMPLIED
	location %string; #IMPLIED
	equipmentId %string; #IMPLIED
>

<!-- Basic transactional elements used throughout -->

<!--
    Defines gross amount in posted currency.
-->
<!ELEMENT TotalAmountInPostedCurrency (Money)>

<!--
    Defines gross amount in billing currency. If specified, customer
    needs to pay this amount.
-->
<!ELEMENT TotalAmountInBillingCurrency (Money)>

<!--
	The bill to for an item.
   
	IdReference: ID reference is Within the application context (for example, a certain pair of buyer 
		and supplier), the (identifier, domain) pair should be unique.
-->
<!ELEMENT BillTo (Address, IdReference*)>

<!--
    The ship to for a item.
	
	Address: To provide a plant location description, the domain attribute of the element IdReference should 
		be set to "buyerLocationID", the identifier to the plantID. The addressIDDomain should be set to 
		"buyerLocationID" and the addressID to the plantID. Use the name for the description.

	IdReference: ID reference is Within the application context (for example, a certain pair of buyer 
		and supplier), the (identifier, domain) pair should be unique.   
-->
<!ELEMENT ShipTo (Address, CarrierIdentifier*, TransportInformation*, IdReference*)>

<!--
The BusinessPartner for an item.

Address: Provides a description of the business party. Follow these instructions for each role:
	<ul>
	<li>soldTo: The domain attribute of the element IdReference should be set to "buyerAccountID", the identifier 
		to the sold to party ID. The addressIDDomain should be set to "buyerAccountID". The addressID should be 
		set to the sold to party ID. Use the name for the description.</li>
	<li>shipFrom: The addressIDDomain should be set to "shipFromAddressID". The addressID should be set to the 
		ship from party ID. Use the name for the description.</li>
	<li>orderingAddress: The addressIDDomain should be set to "supplierCorporate". The addressID should be set 
		to the ordering address party ID. Use the name for the description.</li>
	</ul>

IdReference: ID reference is within the application context (for example, a certain pair of buyer and
         supplier), the (identifier, domain) pair should be unique.
-->
<!ELEMENT BusinessPartner (Address, IdReference*)>

<!--
    @attr type Identifies the type of business partner. Only possible value is organization.
    @attr role Indicates the role the partner plays in the procurement process.
          Possible values are soldTo, shipFrom, orderingAddress, or logistics.
-->
<!ATTLIST BusinessPartner
type             (organization)                         			#REQUIRED
role             (soldTo | shipFrom | orderingAddress | logistics)  #REQUIRED
>

<!--
    Definition of a cXML Shipping item. Represents a shipping cost in the
    shopping basket (PunchOutOrderMessage) or an order to the supplier
    (OrderRequest). There could be one of these for the entire order, or one
    per line item.

    The Modifications element (if present) represents any changes to
    the shipping cost.
-->
<!ELEMENT Shipping (Money, Description, Modifications?)>
<!--
	@attr trackingDomain Represents the logistics supplier, for example, "FedEx", "UPS", and so on.

	@attr trackingId An optional element value that represents the logistics supplier tracking number.

	@attr tracking Deprecated. Do Not Use.
-->
<!ATTLIST Shipping
    trackingDomain  %string;  #IMPLIED
    trackingId      %string;  #IMPLIED
    tracking        %string;  #IMPLIED
>

<!--
    Defines the payment terms in a PaymentProposalRequest document.
	
    Description: A general description on payment terms.

    Extrinsic: Additional information related to this payment terms.
-->
<!ELEMENT PaymentTerms (PaymentTerm*, Description?, Extrinsic*)>
<!--
	@attr paymentTermCode This is the payment term code defined by buyers in ERP.
-->
<!ATTLIST PaymentTerms
    paymentTermCode        %string;         #REQUIRED
>

<!--
    Defines a payment term in an invoice or order. This deprecates the
	InvoiceDetailPaymentTerm previously defined. Payment term can be the 
	net term (without discount) or discount term (with discount).
		
    Discount: The percentage or amount of the discount term. This element should be omitted
	 	if the payment term is a net term.
 
	Extrinsic: Additional information related to this payment term.
-->
<!ELEMENT PaymentTerm (Discount?, Extrinsic*)>
<!--
	@attr payInNumberOfDays The number of days after invoice effective date for the invoice to be paid.
-->
<!ATTLIST PaymentTerm
    payInNumberOfDays        %uint;         #REQUIRED
>

<!--
    The list of valid payment types.
-->
<!ENTITY % cxml.payment  "PCard">


<!--
    The list of valid payment types.
-->
<!ENTITY % cxml.payment  "PaymentToken">


<!--
	Describes the payment instrument used to pay for the items requested. The Payment element 
	contains a PCard element, which encodes a standard purchasing card into the cXML document.
-->
<!ELEMENT Payment (%cxml.payment;)>

<!--
    Defines an accounting segment. Segment is an older, deprecated way to
    transport this information.
-->
<!ELEMENT Segment EMPTY>
<!--
	@attr type The accounting type of this segment.

	@attr id The unique key of this Segment against the type.

	@attr description Textual description of the Segment. For human readability.
-->
<!ATTLIST Segment
    type         %string;  #REQUIRED
    id           %string;  #REQUIRED
    description  %string;  #REQUIRED
>

<!--
    Defines an accounting segment. AccountingSegment is the newer, better
    way to transport this information. Name corresponds to the type
    attribute of Segment; Description corresponds to description. Both add
    required locale attributes to the strings.
-->
<!ELEMENT AccountingSegment ( Name, Description )>
<!--
	@attr id The unique key of this Segment against the type.
-->
<!ATTLIST AccountingSegment
    id           %string;  #REQUIRED
>

<!--
    Accounting entity.
-->
<!ENTITY % cxml.accounting  "( Segment+ | AccountingSegment+ )">
<!--
    An accounting object. Use of the Segment element here is deprecated.
-->
<!ELEMENT Accounting (%cxml.accounting;)>
<!--
	@attr name The name of the object containing the specified accounting segments.
-->
<!ATTLIST Accounting
    name  %string;  #REQUIRED
>

<!--
    A charge against an Accounting element.
-->
<!ELEMENT Charge (Money)>

<!--
    The combination of a Charge against an Accounting Element. A distribution
    represents the breakdown of one overall amount into sub-amounts.
-->
<!ELEMENT Distribution (Accounting, Charge)>



<!-- Item Elements -->
<!--
    The representation of a line item as it needs to be for sending to a
    supplier. The item can refer to a MasterAgreement in itself.
	
    ScheduleLine: Schedule Line(s) listed for the specific line item.
    
    ItemOutIndustry: Industry specific item information.
	
    Packaging: Specify the packaging information for a line item. 
	
    Batch: Batch information of goods or material produced in a single manufacturing run. 
		The information includes ID, characteristic, date, and so on.
-->
<!ELEMENT ItemOut (ItemID, Path?, (ItemDetail|BlanketItemDetail)?, (SupplierID | SupplierList)?, ShipTo?, Shipping?,
                   Tax?, SpendDetail?, Distribution*, Contact*, TermsOfDelivery?, Comments*, Indicator*, Tolerances?, ControlKeys?,
				   ScheduleLine*,(MasterAgreementReference | MasterAgreementIDInfo)?,  ItemOutIndustry?, Packaging*, ReleaseInfo?,  Batch?)>
<!--
	@attr quantity How many items are desired. Supplier and Commodity level items in an order of type "blanket" will use a 
		default value of "1". The quantity in this case will not be used to compute 	the sub total. The MaxAmount in the 
		BlanketItemDetail will be used to indicate any line level amounts.

	@attr lineNumber Position (counting from 1) of this item in an order. Used to maintain a reference between items in 
		create and update OrderRequest documents.

	@attr requisitionID The buyers system requisition id for this line item. It might be the same as orderID, and it might 
	not be included at all. Must not be included if requisitionID is specified in the OrderRequestHeader.

	@attr requestedDeliveryDate The date this item was requested for delivery.

	@attr isAdHoc Indicates the item does not exist in the reference document or contract master agreement.

	@attr agreementItemNumber The corresponding Item Number of the Item in the Master Agreement if this is a "release" order item.

	@attr parentLineNumber lineNumber of the parent line item in the order.

	@attr itemType Specifies the type of item. Possible values:
		<ul>
		<li>composite - Identifies an item group.</li>
		<li>item - Identifies an independent line item.</li>
		<li>lean - Indicates that no child item is expected for the line.</li>
		</ul>

	@attr requiresServiceEntry This is a flag to indicate if a service entry request is required for this item.

	@attr confirmationDueDate Specifies the date by which the supplier has to respond with a Confirmation to the Purchase Order.

	@attr compositeItemType Specify type of parent line item(lot/box).

	@attr itemClassification Specifies whether the current line item is material or service. Valid values are material and service.

	@attr itemCategory It is for defining how a component or material is procured. Possible values are:
		<ul>
		<li>materialUnknown - Refers to procuring a material without specifying the material number.</li>
		<li>text - Refers to procuring a free-form text item.</li>
		<li>stockTransfer - Refers to transfer of stock from one plant to another.</li>
		<li>materialGroup - Refers to procuring a material without specifying the value or quantity.</li>
		<li>subcontract - Refers to process of procuring a material by providing component information that makes the finished product.</li>
		<li>consignment - Refers to process of managing a material through a special process where the payment to supplier is withheld 
			until the material or service is consumed by the buyer.</li>
		<li>thirdParty - Refers to vendor from which a material is ordered.</li>
		<li>limit - Indicates that there is an expected limit for unplanned services or material covered by this item.</li>
		</ul>

	@attr subcontractingType The buyer's ERP system determines the subcontracting type based on the material provision indicator 
		determined to support the regular subcontracting, Refurbishments or Replacements. Possible values:
		<ul>
		<li>regular - Standard subcontracting scenario.</li>
		<li>refurbWithoutchange - For refurbishment without unchanged material.</li>
		<li>refurbWithChange - For refurbishment with changed material.</li>
		<li>replacement - For replacement of the materials.</li>
		</ul>

     @attr stockTransferType The buyer's ERP system sends the following stock transfer types based on the stock transport order or stock transport release.
	    Possible values:
		<ul>
        <li>intra - Refers to transfer of stock from one plant to another plant with in the same Company code.</li>
        <li>inter - Refers to transfer of stock from one plant to another plant  in different company codes.</li>
		</ul>

	@attr requestedShipmentDate The ship date requested by the buyer for the item

	@attr isReturn A value of yes indicates that this item is for return process. Return Items are subject to Order confirmation 
		optionally and also do not support the supplier ASN’s and Goods receipts from Buyers. Return Items is not included in the 
		total of the PO amount of the PO as they are excluded in the case of Mixed line items in single PO.

	@attr returnAuthorizationNumber Return Authorization Number information for a line item.

	@attr isDeliveryCompleted A value of yes indicates that this item is considered closed, it means that no more deliveries are 
		expected for this item. This flag is set by the buyer for informative purposes.
	
	@attr unlimitedDelivery Set to yes to indicate if the line item supports unlimited quantity deliveries. 
	    Corresponding unlimited delivery tolerance is flagged on the ERP.
		
	@attr isItemChanged Indicate if item has changed.

	@attr isKanban The Kanban Indicator indicates the source of the purchase order item, which is, in case if the Kanban Indicator
        is flagged in the purchase order, the Kanban process. If this is the case the buying company wants to indicate the supplier
        that the Kanban related POs require an in time and 100 % delivery quality.
        
    @attr stoDelivery The delivery options possible for stock transport order item.
        <ul>
        <li>full (default) - Only full delivery allowed for the order. This is specific to stock transport order or stock transport release.</li>
        <li>partial - Partial delivery allowed for the order. This is specific to stock transport order or stock transport release.</li>
        </ul>

    @attr stoOrderCombination When the attribute itemCategory has value "stockTransfer" and the attribute stoOrderCombination is set to "yes", 
		then it is possible to combine items from different stock transport orders with the flag set during ship notice creation. 
		A corresponding flag for order combination is set in the buyer's ERP. This setting is relevant for 
		stock transport orders or stock transport releases.

    @attr stoFinalDelivery When the attribute itemCategory has value 'stockTransfer' and the attribute stoFinalDelivery is set to 'yes',
	    then further ship notice creation is not possible. This setting is relevant for stock transport orders or stock transport releases. 

	@attr operation Defines the action associated with the line item. There is only one possible value:
	<ul>
		<li>delete - Removes the line item.</li>
		<li>block - Blocks the line item.</li>
	</ul>
	
-->
<!ATTLIST ItemOut
    quantity               %r8;           #REQUIRED
    lineNumber             %uint;         #IMPLIED
    requisitionID          %string;       #IMPLIED
    agreementItemNumber	   %string;       #IMPLIED
    requestedDeliveryDate  %date;         #IMPLIED
    isAdHoc                (yes)          #IMPLIED
    parentLineNumber       %uint;         #IMPLIED
    itemType               %lineItemType; #IMPLIED
    requiresServiceEntry    (yes)          #IMPLIED
    confirmationDueDate    %date;         #IMPLIED
    compositeItemType      %compositeItemType;	#IMPLIED
	itemClassification     %serviceItemType;	#IMPLIED
    itemCategory           (materialUnknown | text | stockTransfer | materialGroup | subcontract | consignment | thirdParty | limit )       #IMPLIED
    subcontractingType (regular | refurbWithoutChange | refurbWithChange | replacement) #IMPLIED
	stockTransferType      (intra | inter)  #IMPLIED
	requestedShipmentDate  %datetime.tz;  #IMPLIED
    isReturn               (yes)          #IMPLIED
    returnAuthorizationNumber %string;    #IMPLIED
    isDeliveryCompleted    (yes)          #IMPLIED
	unlimitedDelivery      (yes)          #IMPLIED   
	isItemChanged          (yes)          #IMPLIED
	isKanban               (yes)          #IMPLIED
    stoDelivery        	   (full | partial) #IMPLIED
    stoOrderCombination    (yes)          #IMPLIED
    stoFinalDelivery       (yes)          #IMPLIED
    operation              (delete | block) #IMPLIED
>

<!--
   Contains information about the indicator of a line item and if it is a warning or an error.
-->
<!ELEMENT Indicator (#PCDATA)> <!-- string -->
<!-- 
    @attr type Name given to indicator. 

    @attr level Defines if the indicator is a warning or error message. 
-->
<!ATTLIST Indicator
    type    %string;         #IMPLIED
    level   (warning | error)   #IMPLIED
>

<!--
    The representation of the release information of a line item.
-->
<!ELEMENT ReleaseInfo (UnitOfMeasure, ShipNoticeReleaseInfo?, Extrinsic*)>
<!--
	@attr releaseType A string value to identify the type of release. Possible values are "jit" (Just-In-Time)
		or "forecast".

	@attr cumulativeReceivedQuantity A number value to identify the cumulative quantity of all goods 
		receipts against the scheduling agreement over a period up to a certain date

	@attr releaseNumber A string value to identify the release.

	@attr productionGoAheadEndDate Date denoting the end of the production go-ahead period

	@attr materialGoAheadEndDate Date denoting the end of the material go-ahead period (go-ahead for 
		purchase of input materials)
-->
<!ATTLIST ReleaseInfo
    releaseType                 (forecast | jit)  #REQUIRED
    cumulativeReceivedQuantity  %r8;              #REQUIRED
    releaseNumber               %string;          #IMPLIED
    productionGoAheadEndDate    %datetime.tz;     #IMPLIED
    materialGoAheadEndDate      %datetime.tz;     #IMPLIED
>

<!--
    The ShipNoticeReleaseInfo will be used to reference a ship notice created from a scheduling agreement release. 
-->
<!ELEMENT ShipNoticeReleaseInfo ((ShipNoticeReference|ShipNoticeIDInfo), UnitOfMeasure)>
<!--
	@attr receivedQuantity Quantity received by the buyer for the referenced ship notice.
-->
<!ATTLIST ShipNoticeReleaseInfo
    receivedQuantity            %r8;              #REQUIRED
>

<!--
    The representation of a line item as it needs to be for sending to a
    buyer.
    Period: The period during which the service item can be performed.
-->
<!ELEMENT ItemIn (ItemID, Path?, ItemDetail, (SupplierID | SupplierList)?,
                  ShipTo?, Shipping?, Tax?, SpendDetail?, Distribution*,
                  Contact*, Comments?, ScheduleLine*, BillTo?, Batch?, Period?,
                  DateInfo*, Extrinsic*)>
<!--
	@attr quantity How many items are desired.

	@attr openQuantity The quantity pending to be fulfilled by the seller to ship to the buyer. Example: for an Order 
		Quantity of 100, if 50 was delivered, the open quantity is set to 50. The quantity is recorded as undelivered 
		quantity by the buyer.

	@attr promisedQuantity The quantity that has been promised by the selling party. The promised quantity, also 
		called confirmed quantity, will be computed in the seller’s ERP based on ATP functionality. ATP is a function 
		that enables customers’ orders to be confirmed based on available inventory or based on incoming commitments 
		from the suppliers. This quantity will be used to indicate if the Order has been confirmed by the ERP system.

	@attr lineNumber Position (counting from 1) of this item in an order. Used to maintain a reference between items 
		in create and update OrderRequest documents.

	@attr parentLineNumber Position of the parent line item. Used to create a nesting structure for line items.

	@attr itemType Specifies the type of item. Possible values:
		<ul>
		<li>composite - Identifies an item group.</li>
		<li>item - Identifies an independent line item.</li>
		<li>lean - Indicates that no child item is expected for the line.</li>
		</ul>

	@attr compositeItemType Specify type of parent line item(lot/box).

	@attr itemClassification Specifies whether the current line item is material or service. Valid values are 
		material and service.

	@attr itemCategory Defines how a component or material is procured. Possible values are:
		<ul>
		<li>materialUnknown - Refers to procuring a material without specifying the material number.</li>
		<li>text - Refers to procuring a free-form text item.</li>
		<li>stockTransfer - Refers to transfer of stock from one plant to another.</li>
		<li>materialGroup - Refers to procuring a material without specifying the value or quantity.</li>
		<li>subcontract - Refers to process of procuring a material by providing component information that makes the 
			finished product.</li>
		<li>consignment - Refers to process of managing a material through a special process where the payment to supplier 
			is withheld until the material or service is consumed by the buyer.</li>
		<li>thirdParty - Refers to vendor from which a material is ordered.</li>
		<li>limit - Indicates that there is an expected limit for unplanned services or material covered by this item.</li>
		</ul>
-->
<!ATTLIST ItemIn
    quantity         %r8;           #REQUIRED
    openQuantity     %r8;           #IMPLIED
    promisedQuantity %r8;           #IMPLIED
    lineNumber       %uint;         #IMPLIED
    parentLineNumber %uint;         #IMPLIED
    itemType         %lineItemType; #IMPLIED
    compositeItemType      %compositeItemType;	#IMPLIED
    itemClassification     %serviceItemType;	#IMPLIED
    itemCategory           (materialUnknown | text | stockTransfer | materialGroup | subcontract | consignment | thirdParty | limit )       #IMPLIED
>

<!--
	Defines SubcontractingComponent information.
	
	ComponentID: An identifier for a subcontracting component within the procurement process

	UnitOfMeasure: Unit of measure code.

	Description: Description of subcontracting component

	Product: It contains product information of subcontracting component (such as buyer product ID, supplier product ID, 
		standard product ID, internal product ID, and so on)

	ProductRevisionID: An identifier that is assigned when changes are made to component

	Batch: An element carrying a batch information for material or goods  produced in a single manufacturing run 
		(such as buyer/supplier batch ID, production date, property valuation, and so on.)

	SubcontractingComponentIndustry: Contains industry-specific information for SubcontractingComponent.
-->
<!ELEMENT SubcontractingComponent (ComponentID, UnitOfMeasure, Description?, Product?, ProductRevisionID?, Batch?, SubcontractingComponentIndustry?)>
<!--
	@attr quantity Quantity of the subcontracting component required to produce the finished goods in a unit of measurement.

	@attr requirementDate Specifies the date on which the requested quantity of subcontracting component is required.

	@attr materialProvisionIndicator The material provision indicator (MPI) is used to identify the subcontracting 
		type of a part for the components.
-->
<!ATTLIST SubcontractingComponent
	quantity          	%r8;           		#REQUIRED
	requirementDate  	%datetime.tz;  		#IMPLIED
	materialProvisionIndicator (reworkTo | reworkFrom | regular) #IMPLIED
>

<!--
    Defines Schedule Line information.
	
    UnitOfMeasure: Unit of Measure for specified quantity.
		
	deliveryWindow: Duration of time in which the quantity is expected to be delivered.

    SubcontractingComponent: Contains detailed information about a subcontracting component which is used to manufacture 
		the finished goods

    ScheduleLineIndustry: Contains industry-specific information for the schedule line.
-->
<!ELEMENT ScheduleLine (UnitOfMeasure, ScheduleLineReleaseInfo?, SubcontractingComponent*, ShipTo?, ScheduleLineIndustry?, Extrinsic*)>
<!--
	@attr quantity Quantity of items to be shipped for this schedule line. 

	@attr requestedDeliveryDate Date that the specified quantity is expected to be delivered. 

	@attr lineNumber Line identifier for a specific schedule line.

	@attr deliveryWindow Duration of time in which the quantity is expected to be delivered.

	@attr requestedShipmentDate The ship date requested by the buyer for the schedule line

	@attr originalRequestedDeliveryDate The Original Date that the specified quantity is expected to be delivered. 
		This Date is not subject to changes
	
	@attr type Defines the schedule line type when the OrderRequest is a Schedule Agreement Release. Informative only. Possible values:
	<ul>
	<li>backlog - means that the schedule line has a delivery date that lies in the past.</li>
	<li>immediate - means that the schedule line has the delivery date 'today'.</li>
	<li>forecast - means that the schedule line has a delivery date in the future.</li>
	</ul>

	@attr statisticalDeliveryDate The statistical date provided by the buyer at the schedule-line level offers visibility to the supplier on the originally requested delivery date and supports vendor KPI measurement and performance tracking.
-->
<!ATTLIST ScheduleLine
    quantity               %r8;           #REQUIRED
    requestedDeliveryDate  %datetime.tz;  #REQUIRED
	lineNumber  		   %uint;  		  #IMPLIED
	deliveryWindow         %duration;     #IMPLIED
	requestedShipmentDate  %datetime.tz;  #IMPLIED
	originalRequestedDeliveryDate    %datetime.tz;  #IMPLIED
	type                   (backlog | immediate | forecast) #IMPLIED
    statisticalDeliveryDate  %datetime.tz;  #IMPLIED
>

<!--   
    Schedule line industry-specific information. 
    
    ScheduleLineAerospaceAndDefense: schedule line-level Aerospace & Defense industry information, if applicable.

-->
<!ELEMENT ScheduleLineIndustry (ScheduleLineAerospaceAndDefense?)>

<!--
    Aerospace and Defense industry item information

    DPAS: Detailed allocation information of this schedule line in a DPAS-rated order.
          This includes: rating, contract, and quantity information for this schedule line.
-->
<!ELEMENT ScheduleLineAerospaceAndDefense (DPAS*)>
<!--
    @attr dpasRating    Top-level DPAS rating for this schedule line
-->
<!ATTLIST ScheduleLineAerospaceAndDefense
    dpasRating  (DO | DX)       #IMPLIED
>

<!--
    Defines release information on schedule line.
-->
<!ELEMENT ScheduleLineReleaseInfo (UnitOfMeasure)>
<!--
	@attr commitmentCode String value to identify the type of the delivery. Possible values:
		<ul>
		<li>firm - Go ahead for production. Vendor can ship against the schedule line and customer is responsible 
			for cost of production as well as cost of material procurement.</li>
		<li>tradeoff - Go ahead for material procurement. Vendor can ship against the schedule line if rule is enabled. 
			Buyer is responsible for cost of material procurement.</li>
		<li>forecast - Informational. Customer can change the schedule line without incurring any liabilities with the 
			vendor.</li>
		</ul>

	@attr cumulativeScheduledQuantity Total quantity to be shipped for a particular line item up through the schedule line.

	@attr receivedQuantity Received Quantity is the Quantity Received against the Schedule line of the PO or Scheduling 
		Agreement line item based on the Goods Receipt posted in ERP. The quantity is informational and will be only used 
		as visibility for suppliers and will not be validated for Invoice or Shipment processing.
-->
<!ATTLIST ScheduleLineReleaseInfo
    commitmentCode              (firm | tradeoff | forecast)  #REQUIRED
    cumulativeScheduledQuantity %r8;                          #REQUIRED
    receivedQuantity %r8;                                     #IMPLIED 
>

<!--
    StatusUpdate for Confirmation (type=RequestToPay) request.
-->
<!ELEMENT PaymentStatus (PCard, Total, Shipping?, Tax?, Extrinsic*)>
<!--
	@attr orderID Identifies the referenced order. It is copied from the ConfirmationRequest or the OrderRequest.

	@attr transactionTimestamp Time when the XMLPay transaction was submitted.

	@attr type Specifies the type of PCard transaction (enumeration). Possible values:
		<ul>
		<li>Authorization - Authorizes the PCard. No charge is made. There is one authorization per order.</li>
		<li>Settlement - Transfers the funds secured by a previous authorization transaction.</li>
		<li>Sale - Initiates a charge to the PCard.</li>
		<li>Credit - Initiates a credit against the original charge. Compensates for an order that did not meet 
			buyer expectations, to make adjustments to an account that was overcharged, or to credit an 
			account for items returned by a buyer.</li>
		</ul>

	@attr transactionID An identifier assigned to the transaction by the payment processing gateway.

	@attr authorizationID The authorization code for the transaction provided by the bank.

	@attr isFailed Set to "yes" to indicate a failed transaction.
-->
<!ATTLIST PaymentStatus
    orderID    %string;    #REQUIRED
    transactionTimestamp    %datetime.tz;    #REQUIRED
    type    (Authorization| Settlement| Sale| Credit)    #REQUIRED
    isFailed   (yes)    #IMPLIED
    transactionID    %string;    #IMPLIED
    authorizationID    %string;    #IMPLIED
>

<!--
	Partial amount paid against an InvoiceDetail request. Used in InvoiceStatus.
	If this element exists in an InvoiceStatus element, it should mean that the buyer
	does not pay the full amount as the InvoiceDetail request specified. 
-->
<!ELEMENT PartialAmount (Money)>

<!--
	StatusUpdate for InvoiceDetail request.
	
	InvoiceIDInfo: ID of an invoice known to the supplier system. This attribute is used to reference an 
		invoice in StatusUpdateRequest when DocumentReference is omitted.

	PartialAmount: The partial amount paid against the InvoiceDetail document. This attribute is
		only relevant when the status type is "paid".

	Comments: Comments associated with the status update.
-->
<!ELEMENT InvoiceStatus (InvoiceIDInfo?, PartialAmount?, Comments*)>
<!--
	@attr type Type of the invoice status (enumeration). Possible values: 
		<ul>
		<li>processing - The invoice is received and being processed.</li>
		<li>canceled - The invoice has been canceled.</li>
		<li>reconciled - The invoice is reconciled.</li> 
		<li>rejected - The invoice is rejected.</li>
		<li>paying - The invoice is being paid.</li> 
		<li>paid - The InvoiceDetail request is paid.</li>
		</ul>
	@attr paymentNetDueDate The Date after which the invoice has to be paid without any discount.
-->
<!ATTLIST InvoiceStatus
	type	(processing | canceled | reconciled | rejected | paying | paid) #REQUIRED
	paymentNetDueDate  		  %datetime.tz;  #IMPLIED
>
	
<!-- OrderRequest* Elements -->
<!--  
	Specifies the earliest date when either the supplier can deliver the goods or when the receiver is able to handle incoming shipments, 
	and  the latest date when either the supplier can deliver the goods OR until the receiver is able to handle incoming shipments. 
-->
<!ELEMENT DeliveryPeriod (Period)>

<!--
    Definition of an order. This is the data that is sent to the supplier
    to have them place an order in their order management system. The new
    world order equivalent of a PO.
-->
<!ELEMENT OrderRequest (OrderRequestHeader, ItemOut+)>

<!--
    Header of an order. This is the data that is sent to the supplier
    to have them place an order in their order management system. Money
    represents the total amount of this order.
	
    Total: If the Order is of type "blanket", the Total element is not used to compute
		the sum of the item level subtotals. The total is used to indicate the maximum
		commitment with the supplier. The total will not add up to the individual item
		level sub total or MaxAmounts. The item level MaxAmounts should not exceed the
		header level total. If the item level MaxAmount is not specified it is assumed
		that the Item level maximum amount is the same as the Total.
    
	LegalEntity and OrganizationalUnit: Intended to include
		the company code and purchasing organization, respectively.
	
    DeliveryPeriod: Start and end date for the delivery.
     	
    IdReference: ID reference is Within the application context (for example,
		a certain pair of buyer and supplier), the (identifier, domain) pair 
		should be unique.
 
	OrderRequestHeaderIndustry: Industry specific header information.

    DocumentReference: Only needed when the type is "update" or "delete". In that case, 
		the DocumentReference would reference the most recent OrderRequest document
		for the order. For example when an order is created, updated and then
		deleted, the final document should contain a DocumentReference
		referring to the OrderRequest with type="update". That document would,
		in turn, refer to the original (type="new") OrderRequest document.
-->
<!ELEMENT OrderRequestHeader (Total, ShipTo?, BillTo, BusinessPartner*, LegalEntity?,
                              OrganizationalUnit*, Shipping?, Tax?,
                              Payment?, PaymentTerm*, Contact*, Comments*, Followup?, ControlKeys?,
                              DocumentReference?, SupplierOrderInfo?,  TermsOfDelivery?,
                              DeliveryPeriod?, IdReference*, OrderRequestHeaderIndustry?, Extrinsic*)>
<!--
	@attr orderID The buyer system orderID for this request. This is an internal Buyer unique number.

	@attr orderVersion The buyer system order version number for this request. Relevant when the OrderRequest 
		represents a change order request. The version number for the original document should be 1 and should 
		be incremented by 1 for each subsequent version (2,3,4...).

	@attr isInternalVersion A value of yes indicates that this OrderRequest is a version whose changes from the 
		previous version are deemed internal to the buyer system. Relevant when the version being sent to the supplier 
		is not the first version.

	@attr orderDate The date and time the order request was created.

	@attr type The type of the order request. Defaults to "new".

	@attr requisitionID The buyers system requisition id for this entire order. It might be the same as orderID, 
		and it might not be included at all. Must not be included if requisitionID is specified in any ItemOut elements.

	@attr shipComplete Optional preference for "hold until complete" processing. Defaults to shipping when available 
		if not specified. Future versions of the protocol may extend the datatype of this attribute to include additional 
		possible values (such as "unlessGreatlyBackOrdered"?).

	@attr orderType A "release" order type indicates that this is a Release Order from an existing Master Agreement/Contract. 
		Default is regular. Type "blanket" indicate that this order is a Blanket Order. Type "stockTransport" indicates
		that this order is a stock transport order. Type "stockTransportRelease" indicates that this order is a stock transport 
		schedule agreement release (SAR).

	@attr releaseRequired Set to "yes" if the blanket order allows released. Currently used only if orderType is "blanket".

	@attr effectiveDate Date the order is available for ordering. If orderType is "blanket", effectiveDate is required. 
		Otherwise, it is optional. Currently only used with blanket orders.

	@attr expirationDate Date the order is no longer available. If not defined, end date is open ended. Currently only 
		used with blanket orders.

	@attr agreementID Identifies associated agreement corresponding to the Release Order. At an implementation level it 
		has to be validated that if the orderType is "release" then the appropriate agreementID is also provided.

	@attr agreementPayloadID Optional PayloadID for the corresponding Master Agreement.

	@attr parentAgreementID Used only in the context of a Order of type "blanket". This identifies the parent blanket 
		order if the current order is part of an hierarchy.

	@attr parentAgreementPayloadID Optional PayloadID for the corresponding Master Agreement.

	@attr pickUpDate Specifies the date when the goods can be/will be provided for picking up and delivery

	@attr requestedDeliveryDate The requested delivery date reflects the time when the buyer is willing to receive the goods.
    
    @attr isSTOOutbound Set to "yes" to indicate that this OrderRequest is used to send the stock to the supplier. 
		The goods are sent from the buyer plant to the supplier plant. This is currently relevant only for the stock transport order scenario.
-->
<!ATTLIST OrderRequestHeader
    orderID    %string;        #REQUIRED
    orderDate  %datetime.tz;   #REQUIRED
    orderType  (release| regular | blanket | stockTransport | stockTransportRelease) "regular"
    releaseRequired (yes) #IMPLIED
    type       (new | update | delete)  "new"
    orderVersion %number;      #IMPLIED
    isInternalVersion  (yes)   #IMPLIED
    parentAgreementID %string;       #IMPLIED
    parentAgreementPayloadID %string; #IMPLIED
    effectiveDate  %datetime.tz;     #IMPLIED
    expirationDate %datetime.tz;    #IMPLIED
    agreementID %string;       #IMPLIED
    agreementPayloadID %string; #IMPLIED
    requisitionID   %string;   #IMPLIED
    shipComplete    (yes)      #IMPLIED
    pickUpDate    %datetime.tz;   #IMPLIED
    requestedDeliveryDate     %datetime.tz;   #IMPLIED
    isSTOOutbound  	(yes)      #IMPLIED
>
<!--
    Definition of a Master Agreement. This is the electronic document representing the 
    Master Agreement that was created and agreed upon in the Buying organizations.
-->
<!ELEMENT MasterAgreementRequest (MasterAgreementRequestHeader, AgreementItemOut*)>

<!--
   Header of an Agreement. This is the header level information in the Agreement.

   Note: Use "Contact" element to supply any additional Address or Location information.
-->
<!ELEMENT  MasterAgreementRequestHeader (MaxAmount?, MinAmount?, 
					 MaxReleaseAmount?, MinReleaseAmount?, 
					 Contact*,Comments?, DocumentReference?,  Extrinsic*)> 
<!--
	@attr agreementID The buyer system agreementID for this request. the Master Agreement Number in Buyer.

	@attr agreementDate The date and time the agreement request was created. This is different from the 
		effective and expiry date of the agreement.

	@attr type Identifies the type of agreement (enumeration). Possible values:
		<ul>
		<li>value (default) - Value-based agreement.</li>
		<li>quantity - Quantity-based agreement.</li>
		</ul>

	@attr effectiveDate Date the Master Agreement is available for ordering/releases.

	@attr expirationDate Date the Master Agreement is no longer available.

	@attr parentAgreementPayloadID PayloadID for the corresponding parent document that this agreement is derived from.

	@attr operation Type of operation to perform (enumeration). Possible values:
		<ul>
		<li>delete - Cancels an existing Master Agreement, the assumption here is that the 
			delete request will be an exact replica of the original request.</li>
		<li>new - Identifies a new MasterAgreement transaction.</li>
		<li>update - Identifies an update to an existing transaction. The DocumentReference 
			attribute should be used to indicate the Original Document information.</li>
		</ul>
-->
<!ATTLIST MasterAgreementRequestHeader
    agreementID    %string;		   #REQUIRED
    agreementDate  %datetime.tz;	   #REQUIRED
    type	   (value | quantity)	   "value"
    effectiveDate  %datetime.tz;	   #REQUIRED
    expirationDate %datetime.tz;	   #REQUIRED
    parentAgreementPayloadID %string;      #IMPLIED
    operation      (new | update | delete) "new"
>

<!--
    The representation of a  agreement line item as it needs to be for sending to a
    supplier.

    Notes:
		<ul>
		<li>The #lineNumber attribute in the ItemOut will be used to specify the corresponding 
			lineNumber on the Master Agreement in the Procurement Application.
			At an implementation, level checks should be made to validate this.</li>
		<li>The quantity attribute in the ItemOut tag should be set to one and ignored at 
			the Master Agreement implementation processing stage.</li>
		<li>The MaxReleaseAmount/Quantity and MinReleaseAmount/Quantity at an item level 
			indicate the ItemLevel amounts and quantities per release.</li>
		</ul>
-->
<!ELEMENT AgreementItemOut (MaxAmount?, MinAmount?, MaxReleaseAmount?, MinReleaseAmount?, ItemOut)>
<!--
    @attr maxQuantity Maximum quantity for this particular line item.
	
    @attr minQuantity Minimum quantity for this particular line item.
	
    @attr maxReleaseQuantity Maximum quantity per release for this particular line item.
	
    @attr minReleaseQuantity Minimum quantity per release for this particular line item.
-->
<!ATTLIST AgreementItemOut
   maxQuantity		%r8;      #IMPLIED
   minQuantity		%r8;      #IMPLIED
   maxReleaseQuantity	%r8;      #IMPLIED
   minReleaseQuantity	%r8;      #IMPLIED
>

<!--
    The maximum amount for something.
-->
<!ELEMENT MaxAmount (Money)>

<!--
    The minimum amount for something.
-->
<!ELEMENT MinAmount (Money)>

<!--
    The contractual maximum amount per Release of this Master Agreement.
-->
<!ELEMENT MaxReleaseAmount (Money)>

<!--
 The contractual minimum amount per Release of this Master Agreement
-->
<!ELEMENT MinReleaseAmount (Money)>

<!--
    The Overall Limit amount for something.
-->
<!ELEMENT OverallLimit (Money)>

<!--
     The Expected Limit amount for something.
-->
<!ELEMENT ExpectedLimit (Money)>

<!--
    The maximum quantity for something.
-->
<!ELEMENT MaxQuantity (#PCDATA)> <!-- number -->

<!--
    The minimum quantity for something.
-->
<!ELEMENT MinQuantity (#PCDATA)> <!-- number -->

<!--
    The contractual maximum quantity per Release of a contract.
-->
<!ELEMENT MaxReleaseQuantity (#PCDATA)> <!-- number -->

<!--
    The contractual minimum quantity per Release of a contract.
-->
<!ELEMENT MinReleaseQuantity (#PCDATA)> <!-- number -->

<!-- 
	Location to which future StatusUpdateRequest documents should be
    posted. In general, this is the input location for any later
    documents which reference the current OrderRequest document.
-->
<!ELEMENT Followup (URL)>

<!-- PunchOut* Elements -->
<!--
    Definition of a PunchOut Setup Request. This is the data that is sent
    to the external system that the procurement application is going to
    extract catalog data from.

    The BrowserFormPost element contains the URL we would like the browser
    re-directed to when the PunchOut shopping experience is finished (where
    the PunchOutOrder message should be returned).
-->
<!-- PurchaseRequisitionRequest* Elements -->
<!--
    Definition of a purchase requisition. This is the data that is sent from the buyer
    to another buyer system.
-->
<!ELEMENT PurchaseRequisitionRequest (PurchaseRequisition)>

<!-- 
	Contains details of a purchase requisition.
	
	PurchaseRequistionHeader: Header element for the purchase requisition.
	
	ItemIn: Represents an item added from a shopping basket to a requisition 
		in the procurement application.
-->
<!ELEMENT PurchaseRequisition (PurchaseRequisitionHeader, ItemIn*)>

<!--
    Header of a purchase requisition. 
       
    The contained DocumentReference element would appear in a document only
    when the type is "update" or "delete". In that case, the
    DocumentReference would reference the most recent PurchaseRequisitionRequest document
    for the order. For example when an requisition is created, updated and then
    deleted, the final document should contain a DocumentReference
    referring to the PurchaseRequisitionRequest with type="update". That document would,
    in turn, refer to the original (type="new") PurchaseRequisitionRequest document.
-->
<!ELEMENT PurchaseRequisitionHeader (Shipping?, Tax?, Total?, ShipTo?, BillTo?, Contact*, Comments?,
                                     DocumentReference?, Extrinsic*)>
<!--
    @attr requisitionID The buyer system requisitionID for this request. This is an internal
        Buyer unique number.

    @attr requisitionDate The date and time the Requisition request was created.

    @attr type The type of the requisition request (enumeration). Defaults to "new".

    @attr requisitionVersion The buyer system requisition version number for this request. Relevant when
        the PurchaseRequisitionRequest represents a change Requisition request. The version 
        number for the original document should be 1 and should be incremented by 1 for
        each subsequent version (2,3,4, and so on).
-->
<!ATTLIST PurchaseRequisitionHeader
   requisitionID %string;        #IMPLIED
   requisitionDate %datetime.tz;   #IMPLIED
   type       (new | update | delete)  "new"
   requisitionVersion %number;      #IMPLIED
>

<!--
	To initiate a PunchOut session, the user selects the supplier's PunchOut item. The procurement application generates a
	PunchOutSetupRequest document and sends it to a network hub, which forwards it to the supplier's PunchOut website.
-->
<!ELEMENT PunchOutSetupRequest (BuyerCookie, Extrinsic*, BrowserFormPost?,
                                Contact*, SupplierSetup?, ShipTo?,
                                SelectedItem?, ItemOut*)>
<!--
	@attr operation Specifies the type of session the buyer initiates (enumeration). Possible values:
		<ul>
		<li>create - Generate new shopping carts, which correspond to new purchase requisitions.</li>
		<li>inspect - Reopen previously created shopping carts or RFQs for viewing only. The procurement 
			application sends line-item data as part of the PunchOutSetupRequest. However, after 
			re-instantiating the shopping cart, the PunchOut website does not allow modification 
			of its contents.</li>
		<li>edit - Reopen previously created shopping carts or RFQs for modification. The procurement 
			application sends line-item data as part of the PunchOutSetupRequest. The PunchOut website 
			can use this data to re-instantiate the shopping cart created during the original session.</li>
		<li>source - Generate a RFQ for a sourcing application.</li>
		</ul>
-->
<!ATTLIST PunchOutSetupRequest
    operation  (create | inspect | edit | source)  #REQUIRED
>

<!--
	Transmits information that is opaque to the remote website, but it must be returned to the originator for all subsequent 
	PunchOut operations. This element allows the procurement application to match multiple outstanding PunchOut requests. 
	BuyerCookie is unique per PunchOut session.
-->
<!ELEMENT BuyerCookie ANY> <!-- any valid XML data -->

<!--
	Allows suppliers to specify PunchOut for an entire store or any subset of product offerings. Suppliers can create their 
	catalogs so that SelectedItem leads to store-, aisle-, or product-level PunchOut. Procurement applications can include 
	the SelectedItem element in PunchOutSetupRequest documents, and PunchOut sites can use it to determine which products to 
	display to users. The more specific the item is in the catalog, the less searching users have to do at the supplier's 
	website. If there is no SelectedItem, suppliers should present their entire (store-level) product offerings.
-->
<!ELEMENT SelectedItem (ItemID)>

<!--
	Specifies the URL to which to post the PunchOutSetupRequest. This element is not needed if the network hub 
	knows the supplier’s PunchOut URL.
-->
<!ELEMENT SupplierSetup (URL)>

<!--
	After receiving a PunchOutSetupRequest, the supplier’s website sends a PunchOutSetupResponse. The PunchOutSetupResponse 
	document serves two functions:
	<ul>
	<li>It indicates that the PunchOutSetupRequest was successful.</li>
	<li>It provides the procurement application with a redirect URL to the supplier’s Start Page.</li>
	</ul>
-->
<!ELEMENT PunchOutSetupResponse (StartPage)>

<!--
    Definition of a PunchOut Order Message. This is the data that is sent
    back to the procurement application from the external system that the
    PunchOut Request was targeted at.
-->
<!ELEMENT PunchOutOrderMessage (BuyerCookie, PunchOutOrderMessageHeader,
                                ItemIn*)>

<!--
     Header of a PunchOut Order Request. This is the data that is sent from
     the supplier to transfer the supplier acquired shopping basket back to
     the buyer system.
-->
<!ELEMENT PunchOutOrderMessageHeader (SourcingStatus?, Total, ShipTo?, Shipping?, Tax?, SupplierOrderInfo?)>
<!--
     @attr operationAllowed Highest operation allowed on the PunchOut shopping basket.
          "create" allows only later OrderRequest operations on these items.
          "inspect" adds a PunchOutSetupRequest with operation="inspect".
          And, "edit" allows operation="edit" in that later Setup request.

     @attr quoteStatus "pending"  - Identifies that the transaction is still pending
	  "final" - Identifies that the transaction is complete
-->
<!ATTLIST PunchOutOrderMessageHeader
    operationAllowed  (create | inspect | edit)  #REQUIRED
    quoteStatus (pending|final) "final"
>

<!-- ====
    Other small Request elements.
==== -->

<!--
    cXML statuses
-->
<!ENTITY % cxml.statuses  "(PaymentStatus |
			    SourcingStatus | InvoiceStatus | DocumentStatus | IntegrationStatus)">

<!--
    Request to update the status of an earlier transaction.

    DocumentReference: A reference by payloadID to a cXML document to be updated. This 
		element can be omitted only when InvoiceStatus is specified and
		the InvoiceStatus contains InvoiceIDInfo.
	
	Status: Status of the Response or Message.
-->
<!ELEMENT StatusUpdateRequest  (DocumentReference?, Status, (%cxml.statuses;)?, Extrinsic*)>

<!--
    Status update for document status.

    DocumentInfo: ID of a document entry known to the system. This attribute is used to reference a document in StatusUpdateRequest 
	when DocumentReference is omitted.

    Comments: Comments associated with the status update.
-->
<!ELEMENT DocumentStatus (DocumentInfo?, ItemStatus*, Comments*)>
<!--
    @attr type Type of the document status. The possible values depend on the network hub and the type of document being updated.
-->
<!ATTLIST DocumentStatus
        type              %string;               #REQUIRED
        >

<!--
  When a StatusUpdateRequest is send with a DocumentStatus, then you could send more detailed information about the original item.

  ReferenceDocumentInfo: Contains details of referenced document.
		
  Comments: May be used to have a more user readable description of the code.
-->
<!ELEMENT ItemStatus (ReferenceDocumentInfo, Comments*)>
<!--
	@attr type Specifies which kind of status that item should have. Possible values are:
		<ul>
		<li>rejected - The item was not accepted.</li>
		<li>accepted - Original ConfirmationItem is accepted as it was sent.</li>
		</ul>

  @attr code Optional code from the backend ERP system.
  
  @attr parentLineNumber Represents the line number of its parent line number to identify this 
		item's hierarchical parent item in a response message. 
-->
<!ATTLIST ItemStatus
	type        %string;  #REQUIRED
	code        %string;  #IMPLIED
	parentLineNumber       %uint;         #IMPLIED
>

<!--
    Defines the ID of a document known to the system.
-->
<!ELEMENT DocumentInfo EMPTY>
<!--
    @attr documentID The id of a document known to the system.

    @attr documentType The document type.

    @attr documentDate The document date.
-->
<!ATTLIST DocumentInfo
        documentID              %string;               #REQUIRED
        documentType            %string;               #REQUIRED
        documentDate            %datetime.tz;          #IMPLIED
        >

<!--
    A hint to the way to follow up on a QuoteMessage or ContractRequest. The type and category could be set to anything.
    But, they should be well known strings that can be understood by the ERP system.
	
    For example, the type could be set to "Contract" and the category could be set to "WK" or "value".
    This would give a hint to the ERP system that the next step should be to create a WK Contract.
    Furthermore, this element can also be specified in the header section. This allows the UI of Sourcing 
	to be set appropriately.
-->
<!ELEMENT FollowUpDocument EMPTY>
<!--
    @attr type Follow up type such as "Contract", "Scheduling Agreement", and so on.

    @attr category Follow up category such as "WK", "value", and so on.
-->
<!ATTLIST FollowUpDocument
    type          %string;  #IMPLIED
    category      %string;  #IMPLIED
>

<!--
    A reference to an attachment containing cXML document. 

    Attachment: A reference to the attachment containing a cXML document. The 
		attachment must be either a single, complete cXML document or another 
		MIME envelope containing a single, complete cXML document with 
		attachments.
-->
<!ELEMENT cXMLAttachment (Attachment)>

<!--
    Request to forward a cXML document to another party. This Request
    occurs in multiple DTD files and is used depending on where (in which
    DTD) the forwarded message resides.

    cXMLAttachment: A reference to the attachment containing the cXML document being 
		forwarded. Refer to cXMLAttachment for more details.

    cXML: Deprecated. Do Not Use.
-->
<!ELEMENT CopyRequest (cXMLAttachment | cXML)>

<!--
    @attr processingMode Specifies whether the cXML document is meant for processing, 
		information only, or copy:
	<ul>
    <li>info - The cXML document is for information only.</li>
    <li>process - The recipient of the cXML document should process the document.</li>
    <li>copy - The cXML document is a copy as a result of Path element with copy type 
		in the source document.</li>
	</ul>
-->
<!ATTLIST CopyRequest
    processingMode (info | process | copy)  #IMPLIED>

<!--
    Status for a pre-existing sourcing transaction. The textual content indicates
    the display information. 
-->
<!ELEMENT SourcingStatus (#PCDATA)>
<!--
    @attr action Defines the context of this message based on the value. Possible values:
		<ul>
		<li>approve - Approve the pending transaction.</li>
		<li>deny - Deny pending transaction.</li>
		<li>cancel - Cancel any preexisting transaction.</li>
		</ul>
	
	@attr xml:lang The locale used for all free text sent within this document. The receiver 
		should reply or display information in the same or a similar locale.
-->
<!ATTLIST SourcingStatus
	action    (approve | cancel | deny )  #IMPLIED
	xml:lang %xmlLangCode; #REQUIRED>

<!--
	IntegrationStatus is used to allow external parties to provide document status visibility
	after the document is processed and delivered by AN.
-->
<!ELEMENT IntegrationStatus (IntegrationMessage?)>
<!--
	@attr documentStatus Indicates document status (enumeration). Possible values:
		<ul>
		<li>deliverySuccessful - The document was successfully delivered to the customer (but 
			no processing confirmation has been issued yet).</li>
		<li>deliveryDelayed - The document is experiencing delays while trying to reach the 
			customer.</li>
		<li>deliveryFailed - The document could not be sent to the customer due to a failure 
			between the gateway and the customer.</li>
		<li>deliveryReady - When sending to the buyer, the document has been queued and is 
			ready for pick-up.</li>
		<li>customerConfirmed - The customer has confirmed that the document has been processed 
			successfully.</li>
		<li>customerReceived - The customer has confirmed that the document has been received 
			successfully.</li>
		<li>customerFailed - The customer has received the document and is reporting a failure 
			in the content.</li>
		</ul>
-->
<!ATTLIST IntegrationStatus
	documentStatus     (deliverySuccessful | deliveryDelayed | deliveryFailed | deliveryReady
	| customerConfirmed | customerReceived | customerFailed)        #REQUIRED>

<!--
	IntegrationMessage indicates the type/result of the message used by
	external parties.	
-->
<!ELEMENT IntegrationMessage (#PCDATA)>
<!--
	@attr isSuccessful Indicates whether the message result is successful or not.
	
	@attr type Indicates the message type, for example, 997/824/MDN.
-->
<!ATTLIST IntegrationMessage
	isSuccessful	(yes | no)	#REQUIRED
	type	%string;	#REQUIRED>

<!--
    How the shipment will travel on this segment. Each carrier within a
    segment controlled by a third party logistics provider provides
    tracking information to that provider externally. The
    ShipNoticeRequest includes tracking information at the ShipControl
    level only.

    One Route element may describe only a single mode of travel. If
    described at all, each mode of a multi-modal route must be described by
    a separate Route element. It is not necessary to describe every leg of
    the journey to the buyer's ShipTo location.

    The only Contact roles likely to be sensible in this element are
    "carrierCorporate" (details the contact information the supplier has
    about the carrier organization), "shipFrom" and "shipTo". The
    "carrierCorporate" role would be relevant at this level only when a
    third party is providing tracking information across multiple carriers.
    A Contact element with role "shipFrom" must appear in all Route
    elements after the first. Route elements are not required to describe
    the entire travel under a specific carrier's control. They may
    describe a discontinuous stream of events, starting and ending at
    different times and locations.

    Elements in the Contact list may appear in any order. A contact role
    (Contact&#064;role attribute value) must not appear more than once within a
    Route element.
-->
<!ELEMENT Route (#PCDATA | Contact)* >
<!--
    @attr method Qualifier identifying the transportation type code:
		<ul>
		<li>ship - transportation by boat (ocean).</li>
		<li>rail - transportation by rail</li>
		<li>motor - transportation by land motor craft (common carrier)</li>
		<li>air - transportation by flight</li>
		<li>mail - This code is provided for practical reasons, despite the
			fact that mail is not a genuine mode of transport. In many
			countries, the value of merchandise exported and imported
			by mail is considerable, but the exporter or importer concerned
			would be unable to state by which mode postal items had passed
			the national border</li>
		<li>multimodal - This code is provided for practical reasons, despite
			the fact that multimodal transport is not a genuine mode of
			transport. It can be used when goods are carried by at least
			two different modes from a place at which the goods are taken
			in charge by a transport operator to a place designated for
			delivery, on the basis of one transport contract. (Operations
			of pick-up and delivery of goods carried out in the performance
			of a unimodal transport, as defined in such a contract, shall
			not be considered as multimodal transport.)</li>
		<li>fixedTransport - This code applies to installations for continuous
			transport such as pipelines, ropeways and electric power lines.</li>
		<li>inlandWater - This code is used only in such cases where the
			application of waterborne transport is reported separately
			than maritime transport.</li>
		<li>unknown - This code can be used when the mode is not known or when
			information on it is not available at the time of issuing the
			document concerned.</li>
		<li>custom - This code can be used whenever a custom code is used for
			the method. You can put the actual custom code as a text child
			this element.</li>
		</ul>
	
	@attr means Particular vessel, vehicle or other device used for transport of goods.
		The value of means depends on the value of method. Examples include:
		
		When the value of method is "ship", the possible values of means are:
		<ul>
		<li>cargoVessel - Vessel designed to carry general cargo.</li>
		<li>unitCarrier - Vessel designed to carry unit loads.</li>
		<li>bulkCarrier - Vessel designed to carry bulk cargo.</li>
		<li>tanker - Vessel solely equipped with tanks to carry cargo.</li>
		<li>liquefiedGasTanker - Tanker designed to carry liquefied gas.</li>
		<li>otherSpecialTanker - Tanker designed to carry other special liquids.</li>
		<li>cargoAndPassengerVessel - Vessel designed to carry cargo and passengers.</li>
		<li>otherVessel - Sea-going vessel, not otherwise specified.</li>
		<li>fishingBoat - Vessel designed for fishing.</li>
		<li>floatingStructure - Any floating structure.</li>
		</ul>
		
		When the value of method is "rail", the possible values of means are:
		<ul>
		<li>train - One or more rail wagons pulled or pushed by one or more
				locomotive units, or self-propelled, that move over rail tracks.</li>
		<li>freightTrain - Train for carrying freight.</li>
		</ul>
		
		When the value of method is "motor", the possible values of means are:
		<ul>
		<li>truck - Automotive vehicle designed for hauling loads.</li>
		<li>tractor - Automotive vehicle with an engine designed for pulling.</li>
		<li>van - Closed automotive vehicle designed for carrying freight.</li>
		<li>carCarrier - Automotive vehicle designed for carrying motorcars.</li>
		<li>shovelLoader - Automotive vehicle designed for shoveling sand and other bulk material.</li>
		<li>straddleCarrier - Automotive vehicle designed for lifting and transporting containers.</li>
		<li>mobileCrane - Automotive vehicle with cargo crane.</li>
		<li>bus - Automotive vehicle designed for carrying more than 8 passengers including the
				driver.</li>
		<li>car - Automotive vehicle designed to carry a small number of passengers.</li>
		<li>taxi - Automotive vehicle licensed to ply for hire.</li>
		</ul>
		
		When the value of method is "air, the values for means are specified in the
		IATA (International Air Transport Association) publication Standard Schedules
		Information Manual (SSIM) under the section "ATA/IATA Aircraft Types". The
		referenced codes cover all aircraft that are flown, or are soon to be flown,
		for commercial scheduled or chartered services only, or have been announced
		by the manufacturer and for which airline orders have been placed.

		When the value of method is "multimodal": as this includes multiple segments,
		each of which may have different mode and means, the only possible value
		for means is "unknown".
		
		When the value of method is "fixedTransport", the possible values of means are:
		<ul>
		<li>unknown - Fixed transport installation of unknown type.</li>
		<li>pipeline - A line of one or more pipes for continuous transport of
				liquid or gas commodity.</li>
		<li>powerline - A line of one or more cables or wires for continuous
				transport of electricity.</li>
		</ul>
				
		When the value of method is "inlandWater", the possible values of means are:
		<ul>
		<li>unknownVessel - Vessel of unknown type.</li>
		<li>motorFreighter - Motorized vessel designed for carrying general cargo.</li>
		<li>motorTanker - Motorized vessel designed for carrying liquid cargo.</li>
		<li>containerVessel - Vessel designed for carrying containers.</li>
		<li>gasTanker - Vessel with tanks designed for carrying gas</li>
		<li>tug - Vessel designed to push or pull another vessel.</li>
		<li>barge - Lighter designed for carrying general cargo.</li>
		<li>pushTow - Vessel designed for pushing/towing, facilitating the movement
				of one or more cargo barges.</li>
		<li>fishingBoat - Vessel designed for fishing.</li>
		<li>bunkerShip - Vessel designed for carrying and delivering bunkers.</li>
		</ul>

		When the value of method is either "mail" or "unknown", there are no
		specific values of means defined.
		
    @attr startDate The date and time this shipment started this part of the trip.
        Required in all Route elements after the first.

    @attr endDate The date and time this shipment ended this part of the trip. Must
        be after startDate. If any Route elements follow, the startDate of
        that element must not precede this value.
-->
<!ATTLIST Route
    method         (air | motor | rail | ship | mail | multimodal |
					fixedTransport | inlandWater | unknown | custom) 
											   #REQUIRED
	means				 NMTOKEN			   #IMPLIED
    startDate            %datetime.tz;         #IMPLIED
    endDate              %datetime.tz;         #IMPLIED
>

<!--
    Additional information for transportation of the shipment.
    
    Description: Free text description of shipment instructions.
-->
<!ELEMENT ShippingInstructions (Description?)>

<!--
    The shipping contract number specified for the transportation of the shipment.
    
    #PCDATA: The contract number specified by the buyer or seller for the transport. 
-->
<!ELEMENT ShippingContractNumber (#PCDATA)>

<!--
    The terms for transportation of the shipment. This specifies that to whom amongst the buyer and seller
    the title, risk and insurance cost will pass to.
    
    #PCDATA:  Free text description of means of transportation. The text desription must be
        provided if the value "Other" is specified.
-->
<!ELEMENT TransportTerms (#PCDATA)>
<!--
    @attr value The terms of transportation. Typically this may be "Free-Carrier", "CostAndFreight", "DeliveredAtFrontier", 
		and so on, or an option to specify a free text. If the value of this attribute is specified as "other", specifying
        a description is required.
-->
<!ATTLIST TransportTerms
    value %string; #REQUIRED
>

<!--
    The transport information for the shipment described by the order 
    and ship notice. This information comes from the order 
    specified by the buyer and have to be presented in the ship notice
    so that these can be edited by supplier, if required. This information
    and possible changes have to be carried back to the buyer.

    Route: The method or type of transportation of the shipment.

    ShippingContractNumber: Contract number for the transport specified by the buyer or seller.

    ShippingInstructions: Optional instructions for the shipment.
-->
<!ELEMENT TransportInformation (Route?, ShippingContractNumber?, ShippingInstructions?)>

<!--
    The mode of payment for order items shipped. This element is required for 
    specifying terms of delivery.

    #PCDATA: Free text description of shipping payment method. The text description must be
        provided if the value "Other" is specified.
-->
<!ELEMENT ShippingPaymentMethod (#PCDATA)>
<!--
    @attr value The shipping payment method used for the order. Possible values:
        Account, AdvanceCollect, AdvancePrepaid, Other, and so on.
-->
<!ATTLIST ShippingPaymentMethod
    value %string; #REQUIRED
>

<!--
    This element specifies the terms of delivery code. The free text description is optional
    for an order or a ship notice except for the case when "Other" is specified for value.
    
    #PCDATA: Free text description of terms of delivery code. The text description must be
        provided if the value "Other" is specified.
-->
<!ELEMENT TermsOfDeliveryCode (#PCDATA)>
<!--
    @attr value The value of the code that specifies terms of delivery. 
        Indicates whether the terms relate to price conditions, delivery conditions, 
        transport conditions, or a combination of these, or any other.
        If the value of this attribute is specified as other, specifying
        a description is required. Possible values include the following -
        PriceCondition, DespatchCondition, PriceandDespatchCondition, CollectedByCustomer, 
        TransportCondition, DeliveryCondition, Other, and so on.
-->
<!ATTLIST TermsOfDeliveryCode
    value %string; #REQUIRED
>
 
<!--
    The terms of delivery for the shipment described by the order 
    and ship notice. These terms come from the Purchase Order 
    specified by the buyer and have to be presented in the ship notice
    so that these can be edited by supplier, if required. These terms
    and possible changes have to be carried back to the buyer.

    TermsOfDeliveryCode: The code that specifies terms of delivery. Indicates whether the 
		terms relate to price conditions, delivery conditions, transport conditions, or a 
		combination of these, or any other. One must specify description for this if "Other" 
		is specified as the value.

    ShippingPaymentMethod: The mode of payment for order items shipped. Specifying a mode of 
		payment is a must. Examples include: Account, AdvanceCollect, AdvancePrepaid, and so on.

    TransportTerms: This specifies that to whom amongst the buyer and seller the title, risk 
		and insurance cost will pass to.

    Address: Location and city specified with the terms.

    Comments: Optional structured comments can be specified here for multiple types:
		<ul>
        <li>Type "Transport" - Additional description of the transport. Examples include:
            "Delivered To Site / Stores", "OVERNIGHT FIRST CLASS", "To Be Collected by Courier",
			and so on.</li>
		<li>Type "TermsOfDelivery" - Additional delivery instructions can be provided along with
            the other terms.</li>
		</ul>
-->
<!ELEMENT TermsOfDelivery (TermsOfDeliveryCode, ShippingPaymentMethod, TransportTerms?, Address?, Comments*)>

<!--
	A seal is used to preserve integrity of a transport or cargo shipment. Seals come in different versions and 
	technical approaches with one common characteristic: a unique ID given by the owner or the responsible party. 
	The SealID is used to internationally track a container, truck, vessel or other cargo property along the route.
-->
<!ELEMENT SealID (#PCDATA)>
<!--
	The company code for the party that has given the seal in its ID (usually the owner of the goods or the 
	freight forwarder that loaded the cargo.
-->
<!ELEMENT SealingPartyCode (#PCDATA)>
<!--
	Mainly for internal transport and storage purposes the packing equipment is or will be marked with unique 
	codes in order to monitor and manage movement and storage location. Coding can be temporarily or permanent 
	(with GUID-like codes or RFID coding, but unique within the local addressing scheme.
-->
<!ELEMENT EquipmentIdentificationCode (#PCDATA)>
<!--
    The terms for transportation of the shipment. 

	SealID: A seal is used to preserve integrity of a transport or cargo shipment. Seals come in different 
		versions and technical approaches with one common characteristic: a unique ID given by the owner or 
		the responsible party. The SealID is used to internationally track a container, truck, vessel or other 
		cargo property along the route.
	
	SealingPartyCode: Is the company code for the party that has given the seal in its ID (usually the owner 
		of the goods or the freight forwarder that loaded the cargo.
	
	EquipmentIdentificationCode: Mainly for internal transport and storage purposes the packing equipment is 
		or will be marked with unique codes in order to monitor and manage movement and storage location. Coding 
		can be temporarily or permanent (with GUID-like codes or RFID coding, but unique within the local addressing 
		scheme.
-->
<!ELEMENT TermsOfTransport (SealID?, SealingPartyCode?, EquipmentIdentificationCode?, TransportTerms?, Dimension*, Extrinsic*)>

<!--
	For information of referenced document.

	DocumentInfo: The element provides info (such id, type and date) about a document. Following are the 
		possible document types:
		<ul>
		<li>salesOrder - Refers to sales order triggering the PO.</li>
		<li>customerOrder - Refers to the customer order maintained in sales order.</li>
		<li>freightOrder - Refers to order for transportation.</li>
		<li>productionOrder - Refers to order that specifies  which material is to be produced, where it is 
			to be produced, which operations are required to do this  and on which date production is to 
			take place. It also defines how the order costs are to be settled.</li>
		<li>purchaseOrder - Refers to Orders used to replenish based on Supplier Managed Inventory process.</li>
		</ul>
	
	DocumentReference: Reference to an earlier document (for example, OrderRequest). 

	DateInfo: Information of date associated with the document/item. Following are the possible dates:
		<ul>
		<li>expectecDeliveryDate - Refers to the expected delivery date for the material to be received at the 
			partner location.
		<li>expectedShipmentDate - Refers to the expected ship date for the material is used to determine when 
			the supplier is expected to ship the material from their location.</li>
		<li>productionStartDate - Refers to the production start date.</li>
		<li>productionFinishDate - Refers to the production finish date.</li>
		</ul>

	Contact: It is used for contact information for the referenced document
-->
<!ELEMENT ReferenceDocumentInfo ((DocumentInfo | DocumentReference)?, DateInfo*, Contact*, Extrinsic*)>
<!--
	@attr lineNumber Line number of an item in the referenced document.

    @attr scheduleLineNumber Schedule line number of a schedule item in the referenced lineNumber, if this 
		attribute has a value, the lineNumber attribute is required. 

	@attr status Status used to refer to the referenced document.
-->
<!ATTLIST ReferenceDocumentInfo
	lineNumber          %uint;      #IMPLIED
    scheduleLineNumber  %uint;      #IMPLIED
	status (created | released | open | completed | closed | cancelled )     #IMPLIED                       
>

<!--
	Contains date information.
-->
<!ELEMENT DateInfo (Extrinsic*)>
<!--
	@attr type Date type. Possible values:
		<ul>
		<li>expectedShipmentDate - The expected ship date for the material is
			used to determine when the supplier is expected to ship the material
			from their location.</li>
		<li>productionStartDate - The production start date.</li>
		<li>productionFinishDate - The production finish date.</li>
		<li>requestedPickUpDate - The pick up date as requested by the party
			arranging the transport.</li>
		<li>expectedPickUpDate - The date when the logistic provider expects to
			pick up the materials.</li>
		<li>actualPickUpDate - The actual pick up date, as it has occurred.</li>		
		<li>requestedDeliveryDate - The delivery date as requested by the party
			arranging the transport.</li>
		<li>expectedDeliveryDate - The expected delivery date for the material
			to be received at the partner location.</li>
		<li>actualDeliveryDate - The actual delivery date of the goods..</li>
		<li>confirmedDeliveryDate - The delivery date that has been confirmed
			to be delivered by the seller.</li>
		<li>confirmedShipmentDate - The shipment date that has been confirmed by
			to be shipped by seller.</li>
		</ul>
	
	@attr date Date value.
-->
<!ATTLIST DateInfo
	type	(expectedShipmentDate | productionStartDate | productionFinishDate |
				requestedPickUpDate | expectedPickUpDate | actualPickUpDate | 
				requestedDeliveryDate | expectedDeliveryDate | actualDeliveryDate |
				confirmedShipmentDate | confirmedDeliveryDate)	#REQUIRED
	date	%datetime.tz;				#REQUIRED
>

<!--
	Priority indicator, used to drive the priority of the orders for the suppliers.
		Example:
		<ul>
		<li>Level 1 is Very High Priority</li>
		<li>Level 2 is High Priority</li> 
		<li>Level 3 is Medium Priority</li>
		<li>Level 4 is Medium Low Priority</li>
		<li>Level 5 is Low Priority</li>
		</ul>

	Description: Description for priority (such as very high, high, medium, medium low, low).
-->
<!ELEMENT Priority (Description?)>
<!--
	@attr level There are 5 levels (1, 2, 3, 4, 5). Level 1 has the highest priority. Level 5 has the lowest priority.
	 
	@attr sequence An unique secondary order number for prioritizing items with the same priority level. 
		Two items with the same priority level are not allowed to have the same sequence number.
	
	@attr inventory_level Inventory level shows the \% inventory (buffer) level with respect to target. 
		Possible values: 0.0\% - 100.0\%.
-->
<!ATTLIST Priority
  level           ( 1 | 2 | 3 | 4 | 5  )  #REQUIRED
  sequence        %ui8;                   #IMPLIED
  inventory_level %r8;                    #IMPLIED
>

<!--
	Contains information about a document managed in an external system, such as an ERP. It can be sent with an 
	order to distinguish different business transactions uniquely.

    For example,
	%
    <ExternalDocumentType documentType="ZNB">
        <Description xml:lang="en">Standard Purchase Order</Description>
    </ExternalDocumentType>
	%
-->
<!ELEMENT ExternalDocumentType (Description?)>
<!--
	@attr documentType Specifies the document type from the external system.
-->
<!ATTLIST ExternalDocumentType        
	documentType    %string;    #REQUIRED
>

<!--
	Industry specific header information  

	ReferenceDocumentInfo: Details of referenced document 

	Priority: Priority indicator, it is used to drive the priority of the orders for the suppliers

    QualityInfo: The representation of the quality information for the entire OrderRequest.
	
	AssetInfo: Provides detailed per-unit asset information for a line item.
	
	LifeSciences: Provides life sciences industry specific detail.
	
	DPAS: Header-level DPAS-rating info for this order, if applicable.
	
	AuthorizationLabels: ABAC labels for configuring access to this document.

	AerospaceAndDefense: Provides Aerospace and Defense industry information for the entire OrderRequest.
-->
<!ELEMENT OrderRequestHeaderIndustry (ReferenceDocumentInfo*, Priority?, ExternalDocumentType?, QualityInfo?, AssetInfo*, LifeSciences?, DPAS?, AuthorizationLabels?, AerospaceAndDefense?)>
<!--  
    @attr industryType is used to identify the industry for which the document is being sent.
    <ul>
    	<li>lifeSciences</li>
    	<li>aerospaceAndDefense</li>
    </ul>	
-->
<!ATTLIST OrderRequestHeaderIndustry
    industryType	 (lifeSciences | aerospaceAndDefense)	#IMPLIED
>

<!--

	Life sciences industry header information.

-->

<!ELEMENT LifeSciences EMPTY >

<!--
	@attr orderCategory To specify different order categories for clinical trials. Following are possible order categories:
	<ul>
		<li>clinicalFinishedGoodsOrder – Order for clinical finished goods</li>
		<li>labelPrintingOrder – Order for label printing</li>
		<li>reworkOrder – Order for rework</li>
		<li>shelfLifeExtensionRelabelOrder – Order for shelf-life extension relabeling</li>
		<li>additionalLabelPrintingOrder – Order for printing additional labels</li>
	</ul>
-->

<!ATTLIST LifeSciences

orderCategory ( clinicalFinishedGoodsOrder | labelPrintingOrder | reworkOrder | shelfLifeExtensionRelabelOrder | additionalLabelPrintingOrder ) #IMPLIED

>

<!--   
	Industry specific item information. 
	
	ReferenceDocumentInfo: Contains the details of referenced document

	Priority: Priority Indicator used to drive the priority of the item ordered by the buyer.

    QualityInfo: The representation of the quality information of a line item.
    
    SerialNumberInfo: The representation of the required serial number information of a line item.
	
	AssetInfo: Provides detailed per-unit asset information for a line item.
    
    PackagingDistribution: Provides packaging distribution information between stores.
    
    ItemOutAerospaceAndDefense: Item-level Aerospace & Defense industry information, if applicable.

    IUIDInfo: Provides item unique identification information.
-->
<!ELEMENT ItemOutIndustry (ItemOutRetail?, ReferenceDocumentInfo*, Priority?, QualityInfo?, SerialNumberInfo?, BatchInfo?, 
AssetInfo*, PackagingDistribution*, ItemOutLifeSciences?, ItemOutAerospaceAndDefense?, IUIDInfo?)>
<!--
	@attr planningType To specify different production planning strategies. Following are possible strategies:
	<ul>
	<li>MTO - Make to Order</li>
	<li>MTS - Make to Stock</li>
	<li>ATO - Assemble to Order</li>
	<li>CTO - Configure to Order</li>
	</ul>
	
	@attr requiresRealTimeConsumption Set to "yes" to indicate that a real time consumption is required for this item. 
    
    @attr isHUMandatory Set to "yes" to indicate that a handling unit is required for this item. A handling unit is 
		a physical unit consisting of packaging materials (load carriers/packing material) and the goods contained 
		on or in it.
-->
<!ATTLIST ItemOutIndustry
    planningType	 ( MTO | MTS | ATO | CTO )	#IMPLIED
    requiresRealTimeConsumption   (yes)         #IMPLIED
    isHUMandatory                 (yes)		    #IMPLIED
>

<!--
    Aerospace and Defense industry item information

    DPAS: Detailed allocation information of this line item in a DPAS-rated order.
          This includes: rating, contract, and quantity information for this line item.
 -->
<!ELEMENT ItemOutAerospaceAndDefense (DPAS*)>
<!--
    @attr dpasRating    Top-level DPAS rating for this item
-->
<!ATTLIST ItemOutAerospaceAndDefense
    dpasRating  (DO | DX)       #IMPLIED
>

<!--
	This element provides item unique identification information.

	IUIDNumber: A unique identification number for the item.
-->
<!ELEMENT IUIDInfo (IUIDNumber*)>

<!--
	@attr requiresIUID  A value of "yes" indicates that this item requires IUID information.
-->
<!ATTLIST IUIDInfo
	requiresIUID  (yes)  #IMPLIED
>

<!--
	A unique identification number for the item.
-->
<!ELEMENT IUIDNumber (#PCDATA)>

<!--  
	Retail industry item information.
	
	PromotionVariantID: Used when only one or some variants of an article are promoted.

	PromotionDealID: Number assigned by a supplier to a special promotion activity. Promotions affect the forward 
		planning/ordering process (and the related pricing).
-->
<!ELEMENT ItemOutRetail (PromotionVariantID?, PromotionDealID?)> 

<!--
    Details about the packaging of this line item.

    Zero or more PackagingCode elements may appear in the Packaging
    element. Each PackagingCode must contain a single string corresponding
    to the packaging for this item. When multiple PackagingCode elements
    appear, all must describe the same packaging in different languages or
    locales. (No two PackagingCode elements may have the same xml:lang
    attribute.) Elements in such a list may appear in any order.

    The dimensions mentioned in the Dimension element list may appear in
    any order. A particular dimension (Dimension&#064;type attribute value)
    must not appear more than once in a Packaging element.
    
    Description: The Package description.

	PackagingLevelCode: Describes the level (inner, outer, intermediate) for packages.
	
	PackageTypeCodeIdentifierCode: Unique ID of packaging material. This helps receiving side for 
		unloading and storage.
	
	ShippingContainerSerialCode: Serial number of a package that helps to identify package during 
		transportation and inventory.
    
    ShippingContainerSerialCodeReference: Reference from a package with shipping code to the shipping 
		code of the next higher package level. It references the parent ShippingContainerSerialCode.
	
	PackageID: Package related IDs.
	
	ShippingMark: Information about packaging proposals and packaging hierarchy.
	
	OrderedQuantity: The quantity ordered in purchase order.
	
	DispatchQuantity: The dispatch quantity in package.
	
	FreeGoodsQuantity: Quantities delivered but not charged. 
	
	QuantityVarianceNote: Quantity Variance Note gives detailed information about partial delivery.
	
	BestBeforeDate: Gives the best before date for all goods related to food, drugs, chemicals 
		and others.
	
	AssetInfo: The list of asset information of the packaging.

	PackagingIndustry: An element for industry relevant information.
    
    StoreCode: The code of the store assigned for this packaging.
-->
<!ELEMENT Packaging (((PackagingCode+, Dimension*) | Dimension+), Description?, 
				PackagingLevelCode?, PackageTypeCodeIdentifierCode?, ShippingContainerSerialCode?, 
                ShippingContainerSerialCodeReference?,
				PackageID?, ShippingMark*, OrderedQuantity?, DispatchQuantity?, 
				FreeGoodsQuantity?, QuantityVarianceNote?, BestBeforeDate?, AssetInfo*, PackagingIndustry?, StoreCode?, Extrinsic*)>

<!--
    One language-specific code for the packaging of this item. Values such
    as "pallet", "skid" and "truck load" may be appropriate (for an
    English-based locale).
-->
<!ELEMENT PackagingCode (#PCDATA)>             <!-- string -->
<!--
    @attr xml:lang The language or locale in which the PackagingCode content (the
        value of the code) is written.
-->
<!ATTLIST PackagingCode
    xml:lang             %xmlLangCode;         #REQUIRED
>

<!--
    A single dimension for the packaging. It also can be used to define item dimensions.
-->
<!ELEMENT Dimension (UnitOfMeasure)>
<!--
    @attr quantity Size in this dimension. Expressed in the units given in the UnitOfMeasure element.

    @attr type Type of dimension. Possible values:
        <ul>
		<li>length - The length of the packaging or item.</li>
        <li>width - The width of the packaging or item.</li>
        <li>height - The height of the packaging or item.</li>
        <li>weight - The weight  or net weight of the packaging or item.</li>
        <li>volume - The volume or net volume of the packaging or item.</li>
        <li>stackHeight - The stack height of packaging. This indicates total height of the stacked packages.</li> 
        <li>grossWeight - The gross weight is the total weight including packaging.</li>
        <li>grossVolume - The gross volume is the total volume including packaging. </li>
        <li>unitGrossWeight - The unit gross weight of the item.</li>
        <li>unitNetWeight - The unit net weight of the item.</li>
		</ul>
-->
<!ATTLIST Dimension
    quantity             %r8;                        #REQUIRED
    type (length | width | height | weight | volume | stackHeight | grossWeight | grossVolume | unitGrossWeight | unitNetWeight ) #REQUIRED
>


<!-- 
	Describes the level (inner, outer, intermediate) for packages. It is used in logistics for loading 
	and unloading information. This information qualifies the packing level within the packing hierarchy. 
-->
<!ELEMENT PackagingLevelCode (#PCDATA)>

<!-- 
	Provides a unique ID of packaging materials like box, container, pallet and rack. This helps 
	receiving side for unloading and storage. This also defines the maximum load or weight of articles. 
-->
<!ELEMENT PackageTypeCodeIdentifierCode (#PCDATA)>

<!--
	Identifies a package during transport and in inventory. Serial number of a packaging unit like 
	container, box, and rack.
-->
<!ELEMENT ShippingContainerSerialCode (#PCDATA)>

<!--
	Reference from a package with shipping code to the shipping code of the next higher package level. 
	It references the parent ShippingContainerSerialCode.
-->
<!ELEMENT ShippingContainerSerialCodeReference (#PCDATA)>


<!-- 
	Unique ID for a package. GIAI, numbering scheme of GS1 specifying the ownership of an asset.
-->
<!ELEMENT GlobalIndividualAssetID (#PCDATA)>


<!-- 
	The ID that helps supplier with handling of any returned package.
-->
<!ELEMENT ReturnablePackageID (#PCDATA)>

<!-- 
	The information to track packages based on an internal numbering scheme of the supplier. 
-->
<!ELEMENT PackageTrackingID (#PCDATA)>

<!-- 
	Packaging related IDs.
-->
<!ELEMENT PackageID (GlobalIndividualAssetID?,ReturnablePackageID?,PackageTrackingID?)>

<!-- 
	Contains codes for packaging proposals and packaging hierarchy. Also can contain marking 
	for containers/packages that need special signing or handling (hazardous goods, 
	explosives and maximum stack size)
-->
<!ELEMENT ShippingMark (#PCDATA)>


<!-- 
	Specifies the number of items/products for a given line item in a purchase order. 
-->
<!ELEMENT OrderedQuantity (UnitOfMeasure?) >
<!--
	@attr quantity The number of items ordered.
-->
<!ATTLIST OrderedQuantity
    quantity	%r8;	#IMPLIED>

<!-- 
	Specifies the delivered quantity (compared to the ordered quantity). This is useful in 
	determining the correctness of any shipment. 
-->
<!ELEMENT DispatchQuantity (UnitOfMeasure?)  >
<!--
	@attr quantity The number of items delivered.
-->
<!ATTLIST DispatchQuantity
    quantity        %r8;       #IMPLIED>

<!--  
	Specifies the quantity that will be delivered without any cost to the buyer. For example, 
	samples, redemptions, promotions, fill-ups, and so on. These do not appear on the commercial 
	invoice or marked with value 0.00.  
-->
<!ELEMENT FreeGoodsQuantity (UnitOfMeasure?)  >
<!--
	@attr quantity The number of free items.
-->
<!ATTLIST FreeGoodsQuantity
    quantity        %r8;       #IMPLIED>

<!-- 
	Detailed information about partial delivery. Used for different measurements (1 lot = 500 pieces) 
-->
<!ELEMENT QuantityVarianceNote (#PCDATA)>

<!-- 
	The best before date helps with in FDA-related deliveries for go/no-go criteria. This can be 
	used to indicate best before date for all goods related to food, drugs, chemicals, and so on.
-->
<!ELEMENT BestBeforeDate EMPTY>
<!--
	@attr date Best before date.
-->
<!ATTLIST BestBeforeDate
      date         %datetime.tz;    #REQUIRED>
      
<!-- 
	The expiry date  can be used to indicate expiry date for all goods related to food, drugs, 
	chemicals, and so on. 
-->
<!ELEMENT ExpiryDate EMPTY>
<!-- 
	@attr date Expiration date.
-->
<!ATTLIST ExpiryDate
      date         %datetime.tz;    #REQUIRED>

<!--
	References a specific item and its relationship with a PO, Invoice and Ship Notice.   
-->
<!ELEMENT ItemInfo (ItemID?, Description?, Classification*,
						(ManufacturerPartID, ManufacturerName)?,
						Country?, SupplierBatchID?, ReferenceDocumentInfo*,
						UnitOfMeasure, Extrinsic*)>
<!--
 	@attr quantity The quantity included in the document. For example, in TransportRequest, this is the 
		quantity contained of this item in a package.
-->
<!ATTLIST ItemInfo
	quantity		%r8;		#REQUIRED
>

<!--
    The representation of the quality information of a line item.

    IdReference: Use this element to send Quality Material Control Code and Inspection Certificate type information.
		For Quality Material Control Code, domain is "controlCode".
        For Inspection Certificate type, domain is "certificateType".
 
    CertificateInfo: Use this element to send a certificate information, here you can declare a certificate as 
		optional or required. Each element supports one certificate. 
-->
<!ELEMENT QualityInfo (IdReference* | CertificateInfo*)>
<!--
    @attr requiresQualityProcess A value of yes indicates that this item requires quality process.
-->
<!ATTLIST QualityInfo
	requiresQualityProcess         (yes)          #IMPLIED
>

<!--
    Control whether the batch information is required or not for an item when Supplier
	creates an order response.
-->
<!ELEMENT BatchInfo EMPTY >
<!--
    @attr requiresBatch A value of "yes" indicates that this item requires Batch number to be provided by 
		the supplier in Batch element in ShipNoticeItem/ConfirmationStatus element of the corresponding 
		ShipNoticeRequest/ConfirmationRequest documents.     
-->
<!ATTLIST BatchInfo
	requiresBatch        (yes)                      #IMPLIED
>

<!--
    The representation of a certificate information.
    
    IdReference: Use this element to send Quality Material Control Code and Inspection Certificate type information.
		For Quality Material Control Code, domain is "controlCode".
        For Inspection Certificate type, domain is "certificateType". 
-->
<!ELEMENT CertificateInfo (IdReference)>
<!--
    @attr isRequired A value of "yes" indicates that this certificate is required.
-->
<!ATTLIST CertificateInfo
	isRequired         (yes)          #IMPLIED
>

<!--
    The ShipNoticeLineItemReference element provides a clear 
    reference to the line item  in a prior ShipNoticeRequest document.
-->
<!ELEMENT ShipNoticeLineItemReference EMPTY>
<!--

    @attr shipNoticeLineNumber The line number of current line item, copied from ShipNoticeRequest. 
-->
<!ATTLIST ShipNoticeLineItemReference
    shipNoticeLineNumber      %uint;                 #REQUIRED
>

<!--
    The representation of the serial number information of a line item.

    SerialNumber: List of serial numbers, this is required when SerialNumberInfo&#064;type is "list".

    PropertyValue: When SerialNumberInfo&#064;type is "range" a minimum and maximum acceptable
        limits should be provided in PropertyValue element, in this case PropertyValue&#064;name 
        should be "range" and PropertyValue&#064;Characteristic should provide the minimum and maximum 
		values in Characteristic&#064;value attribute with "minimum" and "maximum" as domains in 
		Characteristic&#064;domain attribute.

        When SerialNumberInfo&#064;type is "profile" a serial number format should be
        provided in PropertyValue element, in this case PropertyValue&#064;name 
        should be "profile" and PropertyValue&#064;Characteristic should provide the format.
        Characteristic&#064;domain supports "type", "minLength" and "maxLength" as domains to 
		provide these values in Characteristic&#064;value attribute.
		
        When Characteristic&#064;domain is "type", Characteristic&#064;value supports "numeric", 
		"text" and "numericAndText" to provide the serial number format.
		
        When Characteristic&#064;domain is "minLength", Characteristic&#064;value provides the 
		minimum acceptable length of the serial number.
		
        When Characteristic&#064;domain is "maxLength", Characteristic&#064;value provides the 
		maximum acceptable length of the serial number.       
-->
<!ELEMENT SerialNumberInfo (SerialNumber*, PropertyValue*)>
<!--
    @attr requiresSerialNumber A value of "yes" indicates that this item requires serial number to be provided 
		by the supplier in AssetInfo tags in ShipNoticeItem element of the corresponding Advanced Ship Notice.
    
	@attr type Specifies the required serial number. Possible values:
		<ul>
		<li>list - A list of acceptable serial numbers.</li>
        <li>range - A range of acceptable serial numbers, only valid for numeric ranges.</li>
		<li>profile - A format of acceptable serial numbers.</li>
		</ul>
-->
<!ATTLIST SerialNumberInfo
	requiresSerialNumber    (yes)                      #IMPLIED
    type                    (list | profile | range)   #IMPLIED
>

<!--
    Defines one or more serial numbers for accountable items that are
    being invoiced.
-->
<!ELEMENT SerialNumber (#PCDATA)> <!-- string -->

<!--
    Code for the Store, used on the shipping process.
-->
<!ELEMENT StoreCode (#PCDATA)>


<!--   
	Packaging distribution information. 
	
	StoreCode: Code for the store of this packaging distribution information.

	UnitOfMeasure: Unit of measure code.
-->
<!ELEMENT PackagingDistribution (StoreCode, UnitOfMeasure)>
<!--
    @attr quantity Number of this packaging distribution information. Expressed in the units
        given in the UnitOfMeasure element.
-->
<!ATTLIST PackagingDistribution
    quantity             %r8;                 #REQUIRED
>

<!--
    Defines the relationships that exist between suppliers' production facilities and their production 
    facility roles.
	
    ProductionFacilityAssociation: Each ProductionFacilityAssociation element defines a 
        relationship between a production facility and a production facility role.
        A production facility is a supplier unit used for a particular manufacturing 
        process. A production facility role is the stage of a manufacturing process 
        for which a production facility is used.
-->
<!ELEMENT SupplierProductionFacilityRelations (ProductionFacilityAssociation+)>

<!--
    Defines a relationship between a production facility and a production facility role.
	
    ProductionFacility: A supplier unit used for a particular manufacturing process.
	
    OrganizationalUnit: Purchasing organization of the buyer.
	
    ShipTo: Optional Ship-To addressing information for the buyer's plant.
-->
<!ELEMENT ProductionFacilityAssociation (ProductionFacility, OrganizationalUnit, ShipTo?)>

<!--
    @attr operation Operation to be performed. Possible values are:
        <ul>
        <li><code>new</code> - Represents a new production facility association sent to the external system.</li>
        <li><code>update</code> - Represents an update to an existing production facility association.</li>
        <li><code>delete</code> - Represents an instruction to delete this production facility association in the external system.</li>
        </ul>
        <p>The <code>operation</code> attribute does not create or delete objects such as <code>ProductionFacility</code>, 
            <code>ProductionFacilityRole</code>, <code>OrganizationalUnit</code>, or <code>ShipTo</code>. It is used only
            to define relationships between existing production facilities and production facility roles.</p>
-->
<!ATTLIST ProductionFacilityAssociation 
    operation    (new | update | delete)   "new" >

<!--
    Defines a supplier unit used for a particular manufacturing process.
	
    IdReference: Defines an ID reference that identifies a production facility.
	
    ProductionFacilityRole: The stage of a manufacturing process for which a supplier production facility is used.
-->
<!ELEMENT ProductionFacility (IdReference, ProductionFacilityRole)>

<!--
    @attr productionFacilityName Name of the production facility.
-->
<!ATTLIST ProductionFacility 
    name             %string;             #REQUIRED
>

<!--
    Defines the stage of a manufacturing process for which a supplier production facility is used.
    For example, an apparel manufacturing process could have separate production facility 
    roles for cutting, sewing, pressing and folding, finishing and detailing, dyeing, and washing.

    IdReference: Defines an ID reference that identifies a production facility role.
-->
<!ELEMENT ProductionFacilityRole (IdReference)>

<!--
    @attr productionFacilityRole Name of the production facility role.
-->
<!ATTLIST ProductionFacilityRole 
    name             %string;             #REQUIRED
>

<!--
    Unique identifier used to identify group of studies.
-->
<!ELEMENT PoolID (#PCDATA)> <!-- string -->


<!--  
    Life sciences industry item information.
    One of the elements StudyID, ProtocolID or PoolID needs to be sent with a value otherwise the message will be rejected. 
    Also, if the value of the attribute isCFGPooled has a value ‘yes’ then the poolID value should also be provided in the message.
 	
    Study: Contains information about a clinical study. 

    KitType: Contains a unique identifier for a group of clinical finished goods (CFG) that represents the generalization of the stock 
		keeping unit of a specific clinical supply, regardless of country-specific packaging and labeling requirements.

    ProtocolD: Contains a unique external identifier assigned to the protocol. The protocol is the detailed plan of the clinical study.

    PackageControlNumber: Contains an identification number used for blinding the individual batch numbers for an 
		active drug and a placebo.

    PoolID: Contains an alternative to StudyID when several clinical studies are pooled or grouped together. This value 
		is passed only when several clinical studies are pooled.

    MedicationListInfo: Contains a range of medication numbers.
	
	Extrinsic: Contains any additional information related to the ItemOutLifeSciences element.
-->
<!ELEMENT ItemOutLifeSciences (Study?, KitType?, ProtocolID?, PackageControlNumber?, PoolID?,  MedicationListInfo*, Extrinsic*)> 

<!--
    @attr isCFGPooled Identifies whether clinical finished goods are pooled, that is, used in a pool of studies. 
-->
<!ATTLIST ItemOutLifeSciences
    isCFGPooled         (yes)              #IMPLIED
>

<!--  
    Contains information about a clinical study. 

    StudyID: Contains a unique identification number of a clinical study. 

    Description: Description identifying a clinical study.
-->
<!ELEMENT Study (StudyID, Description?)> 

<!--
    Contains a unique identification number of a clinical study.
-->
<!ELEMENT StudyID (#PCDATA)> <!-- string -->

<!--  
    Contains a unique identifier for a group of clinical finished goods (CFG) that represents the generalization of the stock 
	keeping unit of a specific clinical supply, regardless of country-specific packaging and labeling requirements. 

    KitTypeID: Contains an identification number of the kit type.

    Description: Description identifying a kit type.
-->
<!ELEMENT KitType (KitTypeID, Description?)> 

<!--
    Contains an identification number of the kit type.
-->
<!ELEMENT KitTypeID (#PCDATA)> <!-- string -->

<!--
    Contains a unique external identifier assigned to the protocol. The protocol is the detailed plan of the clinical study.
-->
<!ELEMENT ProtocolID (#PCDATA)> <!-- string -->

<!--   
    Contains an identification number used for blinding the individual batch numbers for an active drug and a placebo.
-->
<!ELEMENT PackageControlNumber (#PCDATA)> <!-- string -->

<!--
    @attr packageControlNumberExpiryDate Expiration date of the PackageControlNumber. 
    @attr isPackageControlNumberReleased Set to "yes" when the PackageControlNumber is released. 
-->
<!ATTLIST PackageControlNumber
    packageControlNumberExpiryDate         %datetime.tz;       #REQUIRED
    isPackageControlNumberReleased         (yes)               #IMPLIED
>

<!--
    Contains a set of medication sequence number ranges.
	
    PropertyValue: When MedictionListInfo&#064;type is "range" a minimum and maximum acceptable
		limits should be provided in PropertyValue element, in this case PropertyValue&#064;name 
		should be "range" and PropertyValue&#064;Characteristic should provide the minimum and maximum 
		values in Characteristic&#064;value attribute with "minimum" and "maximum" as domains in 
		Characteristic&#064;domain attribute.
-->
<!ELEMENT MedicationListInfo (PropertyValue*)>

<!--  
    @attr type Specifies the available medication sequence numbers. Possible value:
    <ul>
    <li>range - A range of sequence numbers, only valid for numeric ranges.</li>
    </ul>
-->
<!ATTLIST MedicationListInfo
    type                    (range)   #IMPLIED
>

<!--
    Defines life sciences industry specific information for SubcontractingComponent.
    LabelID: Contains an identification number used in the BOM for artwork of a label.
-->
<!ELEMENT SubcontractingComponentLifeSciences (LabelID?, Extrinsic*)>

<!--
    Defines industry specific information for SubcontractingComponent.
    SubcontractingComponentLifeSciences: Defines life sciences industry specific information for SubcontractingComponent.
    BatchInfo provides information on whether a batch number is required while reporting component consumption.
-->
<!ELEMENT SubcontractingComponentIndustry (SubcontractingComponentLifeSciences?, BatchInfo?)>
<!--
    Contains an identification number used in the BOM for artwork of a label.
-->
<!ELEMENT LabelID (#PCDATA)> <!-- string -->

<!--
    Defines industry specific information for packaging.
    PackagingLifeSciences: Defines life sciences industry specific information for Packaging.
-->
<!ELEMENT PackagingIndustry (PackagingLifeSciences?)>

<!--
    Defines life sciences industry specific information for Packaging.
    MedicationListInfo: Contains a set of medication sequence number ranges.
-->
<!ELEMENT PackagingLifeSciences (MedicationListInfo*)>


