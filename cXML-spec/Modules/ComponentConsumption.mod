<!--
	A request sent by a supplier to the buyer to report the consumption of components during the 
	manufacturing of an ordered item.
-->
<!ELEMENT ComponentConsumptionRequest (ComponentConsumptionHeader, ComponentConsumptionPortion*)>

<!--
	Contains information about this component consumption that is common to all contained portions.

	Comments: Contains additional information about this consumption document. All such data must be 
		intended for human use.

		Elements in the Comments list may appear in any order. The xml:lang attribute may have the 
		same value in multiple Comments elements in the list. The set of Comments with a particular 
		xml:lang value should contain similar content to that for any other xml:lang value present 
		in the list.

	Extrinsic: Contains additional data about this component consumption header.
-->
<!ELEMENT ComponentConsumptionHeader (Comments*, Extrinsic*)>
<!--
	@attr consumptionID An identifier for this particular component consumption document.

	@attr operation The operational mode of component consumption document. Defaults to "new". 
		Update and delete operations are not supported for this document.

	@attr referenceDocumentID The identifier of reference Work Order for which the consumption 
		is reported.

	@attr creationDate The date and time this component consumption document was created.

	@attr lastChangeDate The date and time this component consumption document was last modified.
-->
<!ATTLIST ComponentConsumptionHeader
    consumptionID           %string;              #REQUIRED
    operation 				(new)                 #REQUIRED
    referenceDocumentID   	%string;              #IMPLIED
    creationDate           	%datetime.tz;         #IMPLIED
    lastChangeDate         	%datetime.tz;         #IMPLIED
>

<!--
	Contains details of all component consumptions for a particular OrderRequest 
	(via the OrderReference or MasterAgreementReference element).

	OrderReference: Identifies the corresponding purchase order for which component consumption is reported.

	MasterAgreementReference or MasterAgreementIDInfo: Can be used to identify the corresponding scheduling 
		agreement if the component consumption is generated from a scheduling agreement release.

	ComponentConsumptionItem: Contains details of all consumption items for given order reference.

	Extrinsic: Contains additional data about this component consumption portion.
-->
<!ELEMENT ComponentConsumptionPortion
(OrderReference, (MasterAgreementReference | MasterAgreementIDInfo)?, ComponentConsumptionItem*, Extrinsic*)>

<!--
	Contains details of all consumption for a given order reference item.

	ItemID: Identifies the part ID of sub-contracted purchase order item for which component 
		consumption is reported.

	BuyerBatchID: The batch ID provided by the buyer at the line item level to identify the buyer batch for which the 
		item or the product was produced. The batch id is helpful in traceability.

	SupplierBatchID: The batch ID provided by the supplier at the line item level to identify  the batch in which the item 
		or the product was produced. The batch id is helpful in traceability. Batch numbers (also call "lot number", "variant") 
		are used in several industries to uniquely identify items produced together.

	Contact: In this context, roles that are usually included are: BuyerParty, ProductReceipentParty, ShipFromLocation, 
		and ShipToLocation.

	ComponentConsumptionDetails: Contains details of component consumption for a given PO line item

	ScrapQuantity: Represents the Scrap of a material that is expected to occur during production if the material 
		is a component.

	AssetInfo: The list of asset information of the item.

	Extrinsic: Contains additional data about this component consumption portion.
-->
<!ELEMENT ComponentConsumptionItem (ItemID, UnitOfMeasure?, Batch?, Contact*, Comments*, ComponentConsumptionDetails*, ScrapQuantity?,  AssetInfo*, Extrinsic*)>
<!--
	@attr poLineNumber Purchase order line number associated with this consumption item.
	
	@attr completedIndicator Set to "yes" to indicate that consumption reporting is completed for a PO item.
	
	@attr quantity The quantity of component that is consumed.
-->
<!ATTLIST ComponentConsumptionItem
	poLineNumber           	%uint;                	#REQUIRED
	completedIndicator   	(yes)                   #IMPLIED
	quantity             	%r8;                    #IMPLIED
>

<!--
	Captures details of component consumption for a given PO line item..

	Product: The supplier & buyer part ID of component that is consumed .

	BuyerBatchID: The batch ID provided by the buyer for the component that is consumed

	SupplierBatchID: The batch ID provided by the supplier for the component that is consumed.

	ReferenceDocumentInfo: Details of referenced production order.

	AssetInfo: The list of asset information of the components consumed. The quantity of these elements 
		must match the quantity of component consumed if at least one is specified.

	Dimension: Item dimensions.

	Extrinsic: Contains additional data at schedule line level of component consumption.
-->
<!ELEMENT ComponentConsumptionDetails (Product, UnitOfMeasure, BuyerBatchID?, SupplierBatchID?, ReferenceDocumentInfo*, AssetInfo*, Dimension*, Extrinsic*)>
<!--
	@attr lineNumber The position of a component in the current consumption details.

	@attr quantity The quantity of component that is consumed

	@attr type Type of inventory movement that could cause the suppliers to not consume the material. Possible values:
		<ul>
		<li>blocked - This inventory is not assigned a value and cannot be consumed.</li>
		<li>qualityRestricted - This inventory is not qualified for unrestricted use and cannot be consumed in production.</li>
		<li>scrapped - This inventory is out of date or material has been destroyed during logistics operation.</li>
		</ul>

	@attr usage Specifies whether the supplier uses a component listed in the order. Used only when confirming an order. 
		Possible values are yes (or not specified) or no. In other contexts, usage is not used. For example, when 
		reporting component consumption use type instead.
-->
<!ATTLIST ComponentConsumptionDetails
	lineNumber          %uint;                						#IMPLIED
	quantity            %r8;                  						#REQUIRED
	type        		(blocked | qualityRestricted | scrapped)  	#IMPLIED
	usage               (yes | no)            						#IMPLIED
>
