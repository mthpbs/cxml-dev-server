<!--
    For cXML license agreement information, please see
    http://www.cxml.org/home/license.asp

    $Id$
-->

<!--
    Please note that all amounts at receipt line level should add up
    to the Total specified in ReceiptRequest.
-->

<!--
    Request representing details about a receipt against Purchase Order or Master Agreement
    sent from a buyer to supplier. Provides purchase order references, line item references, 
    quantities and amounts received and, optionally, reference to related ship notices from supplier.

    This request can be used to send receipt information for any portion of all or 
    selected line items from single or multiple purchase orders.

    ReceiptRequestHeader: The header information of this receipt.

    ReceiptOrder: Details about purchase order or master agreement against which this receipt 
		is generated.

    Total: Summary total amount of all receipt line item amounts.       
-->
<!ELEMENT ReceiptRequest (ReceiptRequestHeader,ReceiptOrder+,Total)>

<!-- 
    Defines header information of an ReceiptRequest

    DocumentReference: Reference to an earlier ReceiptRequest.  If the
        attribute ReceiptRequestHeader@operation is "delete",
        the DocumentReference is required and it must reference the
        original ReceiptRequest document (with value "new" for
        its attribute ReceiptRequestHeader@operation) during the
        same receipt process.  The DocumentReference is optional and, if it
        is provided, it must reference the ReceiptRequest which
        is related to the current ReceiptRequest.

    Comments: Textual comments for this receipt.

    Extrinsic: Additional information related to this receipt. Should not duplicate
        anything in ReceiptRequestHeader or ReceiptRequest.

    ShipNoticeIDInfo: References shipment related document identifiers.
-->
<!ELEMENT ReceiptRequestHeader (DocumentReference?, ShipNoticeIDInfo?, Comments?,Extrinsic*)>
<!--
    @attr receiptID A buyer-generated unique identifier for this receipt.

    @attr receiptDate The date and time the items or services were received by the buying 
		organization. This date and time should be earlier than the document’s timestamp.
	
    @attr operation The operation described by this receipt document. Possible values are "new" 
		(the default) and "delete", which cancels an existing receipt specified by the 
		DocumentReference element. The "delete" operation is supported only for component receipts.

    @attr activityStepType An identifier for a business process. The only possible value is         "stockTransfer", representing the stock transport order scenario.

-->
<!ATTLIST ReceiptRequestHeader
	receiptID %string; #REQUIRED
	receiptDate %datetime.tz; #REQUIRED
    operation (new | delete) "new"
	activityStepType (stockTransfer) #IMPLIED
>

<!-- 
    Defines the receipt information of a purchase order or master agreement with item details.
    In this case, an receipt line is an ReceiptItem and its receipt line number is 
    ReceiptItem@receiptLineNumber.

    ReceiptOrderInfo: The reference information of the purchase order or master agreement.

    ReceiptItem: The information of an item in the purchase order or master agreement.
-->
<!ELEMENT ReceiptOrder (ReceiptOrderInfo, ReceiptItem+)>
<!--
    @attr closeForReceiving Set to "yes" to indicate whether the underlying order or master agreement 
		should be closed for receiving on approval of this receipt.

        If this receipt is against a purchase order or release order against a
        master agreement, this flag indicates corresponding order being closed for
        receiving. If this receipt is against a no release master agreement, this 
		flag indicates closing the master agreement for receiving.
-->
<!ATTLIST ReceiptOrder
	closeForReceiving (yes) #IMPLIED
>

<!-- 
    Defines information related to a purchase order, including purchase order
    reference and related master agreement reference, if any.

    The various content options of ReceiptOrderInfo are in order
    of preference, that is, OrderReference is strongly recommended. If
    that is not possible, MasterAgreementReference, MasterAgreementIDInfo,
    OrderIDInfo should be used, in that order.

    If this receipt is against a release order, reference to both order as well
    as the master agreement should be specified. If the receipt is against a 
    no-release master agreement, only the master agreement reference is required.
    If the receipt is against a purchase order, reference to the purchase order
    should be specified.

    OrderReference: The reference to the purchase order being received.

    MasterAgreementReference: The reference to the master agreement being received.

    OrderIDInfo: The buyer system order ID of the order.

    MasterAgreementIDInfo: The buyer system ID of the corresponding master agreement if receiving
		against master agreement.
-->
<!ELEMENT ReceiptOrderInfo
    ( (OrderReference, (MasterAgreementReference | MasterAgreementIDInfo)?) |
      (MasterAgreementReference, OrderIDInfo?) |
      (MasterAgreementIDInfo, OrderIDInfo?) |
      (OrderIDInfo))>

<!-- 
    Defines line item in the ReceiptRequest

    ReceiptItemReference: The references related to this line item.

    UnitRate: The amount(s) to be paid per unit (of specified measure).

    ReceivedAmount: Amount of received goods or services for this line item. The total received amount 
		for the rate. The total received amount must equal to quantity x UnitRate.

    AssetInfo: Optional asset information for this line item. Specified for each quantity of the line item.
        
    DeliveryAddress: Address where goods are received.

    Comments: Textual comments for this line item.

    Batch: An element carrying batch information for material or goods  produced
        in a single manufacturing run.
    
    Classification: Multiple element that can be used to add a classification for Receipt Item Movement Type, 
		Stock Type or other Classification of the item receipt.
-->
<!ELEMENT ReceiptItem (ReceiptItemReference, UnitRate, ReceivedAmount, AssetInfo*, DeliveryAddress?, Comments?, Batch?, Classification*, Extrinsic*)>
<!--
    @attr receiptLineNumber A buyer-defined ID for the current line item. It must be unique across all receipt 
		line items of the same ReceiptRequest document.

    @attr quantity The quantity received for the current receipt line.
	
		Negative values in the quantity attribute indicate corrective action against an order that was received 
		or returned with errors. For example, if there is an order that has one line item with the quantity as 20, 
		and you originally specified the received quantity as 20; later found out the received quantity was only 
		15 and not 20, you can do a negative receiving to correct this mistake.		
		%
		<ReceiptItem receiptLineNumber="1"
			quantity="-5"
			type="received">
		%
    
    @attr type Indicates whether the items are "received" or "returned".

    @attr parentReceiptLineNumber Line number of the parent line item in the receipt request.

	@attr itemType Specifies the type of item. Possible values:
		<ul>
		<li>composite - Identifies an item group.</li>
		<li>item - Identifies an independent line item.</li>
		<li>lean - Indicates that no child item is expected for the line.</li>
		</ul>

	@attr compositeItemType Specifies whether a parent item uses group-level pricing.

    @attr completedIndicator Set to "yes" to indicate that a component ship notice item is considered closed and no more 
		component receipts are expected for it.
-->
<!ATTLIST ReceiptItem
	receiptLineNumber             %uint;                #REQUIRED
	quantity                      %r8;                  #REQUIRED
	type                      (received|returned)       #IMPLIED
    parentReceiptLineNumber       %uint;                #IMPLIED
    itemType                      %lineItemType;        #IMPLIED
    compositeItemType			  %compositeItemType;	#IMPLIED
    completedIndicator        (yes)                     #IMPLIED
>

<!-- 
    Defines all references related to a line item in this receipt.

    ItemID: The supplier part number of current line item, copied from
        OrderRequest.

    Description: The line item description, copied from OrderRequest.

    ManufacturerPartID: The manufacturer part number.

    ManufacturerName: The name of the manufacturer.

    ShipNoticeReference: Reference to the ShipNoticeRequest document sent from the supplier 
        when this item was shipped.

    ShipNoticeIDInfo: ID of the ShipNoticeRequest known to the Buyer system. This ID is used when ShipNoticeReference is omitted.
       
    ShipNoticeLineItemReference: Provides a reference to the line item in a prior ShipNoticeRequest document.

    ReferenceDocumentInfo: Additional document reference information.        
-->
<!ELEMENT ReceiptItemReference (ItemID?, Description?, (ManufacturerPartID, ManufacturerName)?, (ShipNoticeReference | ShipNoticeIDInfo)?, ShipNoticeLineItemReference?, ReferenceDocumentInfo*)>
<!--
	@attr lineNumber The line number of the current line item, copied from the OrderRequest document. The line 
		number can also refer to a line item in the component ship notice referenced by ShipNoticeReference.
-->
<!ATTLIST ReceiptItemReference
	lineNumber %uint; #REQUIRED
>

<!-- 
    Amount of goods or services received by the buying organization.
-->
<!ELEMENT ReceivedAmount (Money)>

<!--
    The delivered address for an item.
-->
<!ELEMENT DeliveryAddress (Address)>

