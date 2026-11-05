<!--
    For cXML license agreement information, please see
    http://www.cxml.org/home/license.asp

    $Id: //ariba/cxml/modules/Item.mod#19 $
-->

<!--
    Type of a line item.

    lean: This line type indicates no child line is expected for line.
-->
<!ENTITY % lineItemType '(composite | item | lean)'>

<!--
    Type of a composite line item. 
-->
<!ENTITY % compositeItemType '(groupLevel | itemLevel)'>

<!--
    Type of a service line item.
-->
<!ENTITY % serviceItemType '(material | service)'>

<!--
    Choice of type of a service line item. This entity represents the type of the service line. The following are the different 
	types of service lines. 
	
	standard: This is the standard line type, and is the system default.
	
	blanket: You do not specify a quantity for blanket lines. The system defines the default quantity one to service lines 
		with this line type. It is settled as a lump sum.
	
	contingency: This line type describes a service that is not absolutely necessary to perform the order. This service 
		is only used when required.
	
	openquantity: If the sold-to party requires that the bidder offers the quantity for a specific partial service,
		the service line can be indicated by Line with open quantity. You can only use this line type in the following 
		documents:
		<ul>
		<li>Purchase requisitions and RFQs in Purchasing (MM Services)</li>
		<li>Inquiries with BOS in Sales</li>
		</ul>
	
	information: This line type does not describe a service. It is purely documentary in nature.
		You select this line type if you want to use the service line to store additional text.
-->
<!ENTITY % serviceLineType '(standard|blanket|contingency|openquantity|information)'>


<!--
	Choice of alternativeTypes.

	No Alternative: This line type describes a service that cannot be performed in alternative ways.

	Basic Line: This line type describes a service that can be performed in alternative ways. 
		There are one or more alternative lines for each basic line. The value of a basic line is 
		included in the total value of the service specifications.

	Alternate Line: An alternative line describes a different way of performing a service or doing work 
		than that set out in the associated basic line.

		The value of an alternative line is not included in the total value of the service 
		specifications.
		
		Example:
		If flooring is to be renewed, you may want to enter one service line for parquetry 
		flooring and an alternative line for ceramic tiles.    
-->
<!ENTITY % alternativeType '(noAlternative | basicLine | alternativeLine)'>

<!--
    Type with which a service line item can be identified to its alternative item. 
-->
<!ELEMENT Alternative EMPTY>
<!--
	@attr alternativeType This represents the alternative type of a service line item. 

	@attr basicLineNumber When an alternative type is set as Alternate Line, the line number of the 
		Service Line which whose alternative type set as Basic Line will be set here. This enables 
		this service line item to identify for which service line item this is alternative to. 
-->
<!ATTLIST Alternative
	alternativeType  %alternativeType;  #REQUIRED
	basicLineNumber  %string;  #IMPLIED
> 

<!--
    ID with which the item's manufacturer identifies the item.
-->
<!ELEMENT ManufacturerPartID (#PCDATA)> <!-- string -->

<!--
    Name of the item's manufacturer.
-->
<!ELEMENT ManufacturerName (#PCDATA)> <!-- string -->
<!-- 
	@attr xml:lang The language in which the ManufacturerName is written. This attribute will be 
		required in a future version of cXML. (Leaving it out is deprecated.) 
-->
<!ATTLIST ManufacturerName
    xml:lang %xmlLangCode; #IMPLIED
>

<!--
    Classification is used to group items into similar categories.
-->
<!ELEMENT Classification (#PCDATA)> <!-- string -->
<!-- 
    @attr domain Name of classification. Groups items into similar categories. Typically lists the UNSPSC 
		(United Nations Standard Products and Services Code) commodity code for each selected item. 
		These codes are used by backend systems within buyer and supplier organizations for accounting 
		and report generation. For the list of UNSPSC codes, see www.unspsc.org. The domain attribute 
		can also be used to specify product hierarchy and commodity information used by a backend system.
		Domain can also be "MaterialGroup", which refers to a grouping of materials and services according 
		to their characteristics in SAP.

	@attr code Identifies the commodity by its designated code. 
-->
<!ATTLIST Classification
    domain  %string;  #REQUIRED
	code 	%string;  #IMPLIED
>

<!--
     Specifies the amount of time in days required to
     receive the item.
-->
<!ELEMENT LeadTime (#PCDATA)>   <!-- uint -->

<!--
    SupplierBatch is the batch ID provided by the supplier at the line item quantity level
    to identify  the batch in which the item or the product was produced. The batch id is helpful in traceability.

    Batch numbers (also call "lot number", "variant") are used in several industries to uniquely identify  items produced together.
-->
<!ELEMENT SupplierBatchID (#PCDATA)>


<!--
    How the supplier identifies an item they sell.

    If SupplierPartID does not provide a unique key to identify the item,
    then the supplier should generate a key which identifies the part
    uniquely when combined with the SupplierID and SupplierPartID. The
    key is called SupplierPartAuxiliaryID.


    An example is where a Supplier would use the same PartID for an
    item but have a different price for units of "EA" versus "BOX".
    In this case, the ItemIDs should be:
	%
    <ItemID>
        <SupplierPartID>pn12345</SupplierPartID>
        <SupplierPartAuxiliaryID>EA</SupplierPartAuxiliaryID>
    </ItemID>
    <ItemID>
        <SupplierPartID>pn12345</SupplierPartID>
        <SupplierPartAuxiliaryID>
           <foo>well formed XML here</foo>
        </SupplierPartAuxiliaryID>
    </ItemID>
	%
    In this case, the "foo" element must be defined in an internal subset
    sent with the cXML document. Otherwise, parsers will not be able to
    validate that document.

    In a preferred approach, the sending application may escape the contained
    XML using CDATA sections. This would appear as:
	%
       ...
       <SupplierPartAuxiliaryID>
           <![CDATA[<foo>well formed XML here</foo>]]>
       </SupplierPartAuxiliaryID>
       ...
	%
    Finally, the angle brackets could be escaped using XML character
    entities. This might be a bit harder for humans to read. For example:
	%
       ...
       <SupplierPartAuxiliaryID>
           &lt;foo&gt;well formed XML here&lt;/foo&gt;
       </SupplierPartAuxiliaryID>
       ...
	%
-->
<!ELEMENT SupplierPartID (#PCDATA)> <!-- string -->
<!-- 
    @attr revisionID Also known as Material Revision Level. "Revision" is the most widely-used
       term for referring to that attribute which distinguishes one closely-related design 
	   iteration from another. A revision represents a change to an object, or a modification 
	   to a part such that the part remains interchangeable with its previous iteration.
       As part of engineering change management the supplier uses the revision level to uniquely 
	   identify the version of a product, of an article or of a part. 
-->     
<!ATTLIST SupplierPartID
	revisionID	%string;	#IMPLIED
>

<!--
	If SupplierPartID does not uniquely identify the item, the supplier should use SupplierPartAuxiliaryID to specify an "auxiliary" 
	key that identifies the part uniquely when combined with the SupplierID and SupplierPartID. For example, a supplier might use 
	the same SupplierPartID for an item, but have a different price for units of "EA" and "BOX". In this case, a reasonable 
	SupplierPartAuxiliaryID for the two items might be "EA" and "BOX."
	
	SupplierPartAuxiliaryID could also be used as a supplier cookie, enabling the supplier to refer to complex configuration or 
	part data. It could contain all the data necessary for the supplier to reconstruct what the item in question is in their 
	computer system (a basket or cookie of data that makes sense only to the supplier).
	
	SupplierPartAuxiliaryID can help a remote website transport complex configuration or bill-of-goods information to re-identify 
	the item when it is presented to the remote website in the future.
	
	If SupplierPartAuxiliaryID contains special characters (for example, if it contains additional XML elements not defined in the 
	cXML protocol), they must be escaped properly. Due to the necessity to pass SupplierPartAuxiliaryID information through 
	applications and back to the originating supplier, an internal subset containing any additional XML elements is insufficient.
-->
<!ELEMENT SupplierPartAuxiliaryID ANY>

<!--
   BuyerPartID represents an item in buyer system. This identifier is specified by the buyer
-->
<!ELEMENT BuyerPartID (#PCDATA)>

<!--
    A unique identification of an item. SupplierID is not required since
    ItemIDs never travel alone.

    ItemIDs used to define a Supplier/Commodity level Blanket Order Item should
    use Empty SupplierPartID tag. These type of items do not have specific part numbers
    at the time of placing the Blanket Order. There will be subsequent release orders
    or invoices that will define the SupplierID.

    IdReference: Defines an ID reference. Within the application context (for example,
		certain pair of buyer and supplier), the (identifier, domain) pair should be unique.
-->
<!ELEMENT ItemID (SupplierPartID, SupplierPartAuxiliaryID?, BuyerPartID?, IdReference*)>

<!--
    ItemDetail contains detailed information about an item. All the data that
    a user would want to see about an item instead of the bare essentials
    that are represented in the ItemID.
	
	LeadTime: Time in days to receive the item.
    
	Dimension: Item dimensions.
    
	ItemDetailIndustry: Industry specific item detail information.

	OverallLimit: Holds the maximum value that the total of all unplanned services 
		(or the value of the material) covered by this item may not exceed. This limit, 
		which represents a budget for unplanned services, must not be exceeded. 
		This limit can be used in service lines and the items of blanket purchase orders.
		At service item level (top level service item), this field captures limit for the 
		total of all unplanned services (or the value of the material). At service 
		outline level, it captures the sub-limits (the budget for that item). 

	ExpectedLimit: ExpectedLimit is value that the unplanned services (or the material) 
		covered by this item are not expected to exceed. It is a figure buyer assumes to be 
		the final amount payable. It is entered only for top level service item. Whereas 
		the overall limit field (at top level) captures the maximum allowed value (max 
		amount that can be spent), the expected limit is used to capture the planned/expected 
		value and is mainly used for analysis purposes and determining total value of item.
    
	PlannedAcceptanceDays: Number of days the buyer schedules for the inspection of goods 
		after receiving them.
-->
<!ELEMENT ItemDetail (UnitPrice, Description+, OverallLimit?, ExpectedLimit?, UnitOfMeasure, PriceBasisQuantity?, Classification+, ManufacturerPartID?,ManufacturerName?, URL?, LeadTime?, Dimension*, ItemDetailIndustry?, AttachmentReference*, PlannedAcceptanceDays?, Extrinsic*)>

<!--
	Item details specific for items that describe blanket purchase order items. 
	This element should only be used for items in a purchase order of 
	type "blanket"	or "regular".
-->
<!ELEMENT BlanketItemDetail (Description+, OverallLimit?, ExpectedLimit?, MaxAmount?, MinAmount?, 
                             MaxContractAmount?, MaxAdhocAmount?, MaxQuantity?, MinQuantity?,
			                 UnitPrice?, UnitOfMeasure?, PriceBasisQuantity?,
			                 Classification*, Extrinsic*)>

<!--
    ItemDetailIndustry contains detailed information about an item for a specific industry.
-->
<!ELEMENT ItemDetailIndustry (ItemDetailRetail? )>
<!-- 
	@attr isConfigurableMaterial A value of yes indicates that this item is defined as a configurable 
		purchase and has material characteristics associated with it.  
-->
<!ATTLIST ItemDetailIndustry
    isConfigurableMaterial	 (yes)  #IMPLIED
>

<!--
	This element provides import-related information for the shipment.

	HTSCode: A classification code used by customs authorities to identify products imported into a country and determine the applicable duties, tariffs, taxes, and
  trade regulations.

	ImporterOfRecord: The individual or organization legally responsible for ensuring imported goods comply with a country's customs and trade regulations.
-->
<!ELEMENT ImportInfo (HTSCode?, ImporterOfRecord?)>

<!--
	Harmonized Tariff Schedule Code. A classification code used by customs authorities to identify products imported into a country and determine the applicable duties, tariffs, taxes, and trade regulations.
-->
<!ELEMENT HTSCode (#PCDATA)>

<!--
	Importer of Record (IOR). The individual or organization legally responsible for ensuring imported goods comply with a country's customs and trade regulations.
-->
<!ELEMENT ImporterOfRecord (#PCDATA)>

<!--
    ItemDetailIndustry contains detailed information about an item for a retail industry. 
    
    EANID: Deprecated. Use '<IdReference domain="EAN-13">' within 'ItemID' instead.
	
	EuropeanWasteCatalogID: Specifies a unique ID for articles listed in the EU Waste 
		Catalog (EWC) if it requires special handling.
		
	Characteristic: Specifies detailed information about an item that can be used across 
		several different industries.
-->
<!ELEMENT ItemDetailRetail (EANID?, EuropeanWasteCatalogID?, Characteristic* )>

<!--
    An identifier for a subcontracting component within the procurement process.
-->
<!ELEMENT ComponentID (#PCDATA)>

<!--
    An element contains product information of subcontracting component (such as buyer product id, supplier 
	product id, standard product id, internal product id, and so on).
	
	SupplierPartID: It represents a product in supplier system. This identifier is specified by the supplier.

    SupplierPartAuxiliaryID: If SupplierPartID does not provide a unique key to identify the item, then the 
		supplier should generate a key which identifies the product uniquely when combined with the SupplierID 
		and SupplierPartID. The key is called SupplierPartAuxiliaryID.

    BuyerPartID: It represents a product in buyer system. This identifier is specified by the buyer.

    IdReference: ID reference is within the application context (for example, standard id for a product), 
		the (identifier, domain) pair should be unique.
-->
<!ELEMENT Product (SupplierPartID?, SupplierPartAuxiliaryID?, BuyerPartID?, IdReference*)>

<!--
    A sequential number that is assigned when changes are made to component.
-->
<!ELEMENT ProductRevisionID (#PCDATA)>

<!--
    An element carrying batch information for material or goods  produced in a single manufacturing run.

    BuyerBatchID: An identifier from buyer to identify the material/goods produced in a single manufacturing run.

    SupplierBatchID: An identifier from supplier to identify the material/goods produced in a single 
		manufacturing run.

    PropertyValuation: Consists of the property to be valuated and the associated values.
-->
<!ELEMENT Batch (BuyerBatchID?, SupplierBatchID?, PropertyValuation*)>
<!-- 
    @attr productionDate Date on which when a batch of material or goods is produced.

    @attr expirationDate Date on which when a batch of material/goods becomes expired.

    @attr inspectionDate Date on which when a batch of material/goods will be inspected.

    @attr shelfLife Duration that a product is expected to remain within its approved product specification 
		after production date. This attribute is used for information purposes only.

    @attr originCountryCode Country of origin for a batch of material or goods.

    @attr batchQuantity Quantity for a batch of material or goods.
-->
<!ATTLIST Batch
    productionDate      %datetime.tz;    	#IMPLIED
    expirationDate   	%datetime.tz;      	#IMPLIED
    inspectionDate      %datetime.tz;       #IMPLIED
    shelfLife           %duration;          #IMPLIED
    originCountryCode 	%isoCountryCode; 	#IMPLIED
    batchQuantity       %r8;                #IMPLIED
>

<!--
    An identifier from buyer to identify the material/goods produced in a single manufacturing run.
-->
<!ELEMENT BuyerBatchID (#PCDATA)>

<!--
    References the property to be valuated and the associated values.
	
	PropertyReference: The property being valuated.

    ValueGroup: Contains a group of values pertaining to a property.
-->
<!ELEMENT PropertyValuation (PropertyReference?, ValueGroup*)>

<!--
    References the property being valuated.

    IdReference: ID reference is within the application context (for example, ID for a property). 
		The (identifier, domain) pair should be unique.
-->
<!ELEMENT PropertyReference (IdReference*)>

<!--
    Contains a group of values pertaining to a property.
	
    IdReference: ID reference is within the application context (for example, ID for a property). 
		The (identifier, domain) pair should be unique.

    ParentID: For Structured property, ParentID is an identifier of its parent.

    PropertyValue: Values for valuating property.
-->
<!ELEMENT ValueGroup (IdReference?,  ParentID?, PropertyValue*)>

<!--
	For Structured property, ParentID is an identifier of its parent.
-->
<!ELEMENT ParentID (#PCDATA)>

<!--
    Contains values for valuating a property.
	
    Examples:
	<ul>
	<li>QuantitySpecification - valuating properties whose data types are in units, for example, 5 pieces, 7 kg.</li>
    <li>IntegerSpecification - valuating non-dimensional, integer properties, for example, codes, indexes, and sequential numbers.</li>
	<li>DateTimeSpecification - valuating date time properties, for example requirement date, production date, and expiration date.</li>
	</ul>
	
	Characteristic: Contains detailed information about a value that can be used for valuation of property
-->
<!ELEMENT PropertyValue (Characteristic*)>
<!-- 
	@attr name Name of PropertyValue.
-->
<!ATTLIST PropertyValue
	name          %string;       #IMPLIED>

<!--
	ControlKeys are used to override default business rules as configured in the network hub.
-->
<!ELEMENT ControlKeys (OCInstruction?, ASNInstruction?, InvoiceInstruction?, SESInstruction?)>

<!--
	Indicates whether an order confirmation is allowed for this order or line item, regardless of the default business 
	rules configured in the network hub.
	
	Lower: Specifies tolerances that define a lower limit.
	
	Upper: Specifies tolerances that define an upper limit.
-->
<!ELEMENT OCInstruction (Lower?, Upper?)>
<!--
	@attr value Value that indicates whether an order confirmation is allowed. Possible values:
		<ul>
		<li>allowed - Order confirmation is allowed.</li>
		<li>notAllowed - Order confirmation is not allowed.</li>
		<li>requiredBeforeASN - Order confirmation is required before a ship notice.</li>
		</ul>
-->
<!ATTLIST OCInstruction
	value     ( allowed | notAllowed | requiredBeforeASN )     #REQUIRED>

<!--
	Indicates whether a ship notice is allowed for this order or line item, regardless of the 
	default business rules configured in the network hub.
	
	Lower: Specifies tolerances that define a lower limit.
	
	Upper: Specifies tolerances that define an upper limit.
-->
<!ELEMENT ASNInstruction (Lower?, Upper?)>
<!--
	@attr value Value that indicates whether a ship notice is allowed. Possible values:
		<ul>
		<li>allowed - Ship notice is allowed.</li>
		<li>notAllowed - Ship notice is not allowed.</li>
		</ul>
-->
<!ATTLIST ASNInstruction
    value       (allowed | notAllowed)  #REQUIRED>

<!--
	Indicates whether an invoice is allowed for this order or line item, regardless of the default 
	business rules configured in the network hub.
	
	TemporaryPrice: Indicates if pricing information is temporary or final. It can be set at the 
		order header or item level. It has a required value attribute, which can be set to "yes" or 
		"no". If set to "yes" at the order item level, pricing for the item is considered temporary 
		and the supplier cannot invoice the line item. If set to "yes" at the order header level, 
		pricing is considered temporary for the entire purchase order and the supplier cannot 
		invoice against any item on the order.
	
	Lower: Specifies tolerances that define a lower limit.
	
	Upper: Specifies tolerances that define an upper limit.
-->
<!ELEMENT InvoiceInstruction (TemporaryPrice?, Lower?, Upper?)>
<!--
	@attr value Value that indicates whether an invoice is allowed. Possible values:
		<ul>
		<li>isERS - The order or line item is flagged for Evaluated Receipt Settlement, indicating that the 
			system will post an invoice for it automatically based on goods receipts.</li>
		<li>isNotERS - The order or line item is not flagged for Evaluated Receipt Settlement.</li>
		<li>allowed - Invoice is allowed.</li>
		<li>notAllowed - Invoice is not allowed.</li>
		</ul>

	@attr verificationType The only supported value is "goodsReceipt", which indicates that invoice verification 
		for this item is based on the goods receipt. The invoice item can then be matched up uniquely with the 
		goods receipt item. Goods-receipt-based invoice verification makes sense when you expect a delivery to 
		be made and posted in several parts.

	@attr unitPriceEditable Specifies whether a buyer or supplier is allowed to update the unit price while 
		creating an invoice. Possible values:
		<ul>
		<li>yes - Buyer or supplier is allowed to edit unit price. The new price must be within price tolerance.</li>
		<li>no - The unit price is not editable regardless of transaction rules defined in the network hub.</li>
		</ul>

		If the unitPriceEditable attribute is not present, the system honors the default transaction rules 
		present in the network hub.
-->
<!ATTLIST InvoiceInstruction
    value       (isERS | isNotERS | allowed | notAllowed)  #IMPLIED
    verificationType (goodsReceipt)                        #IMPLIED
    unitPriceEditable (yes | no) 			   #IMPLIED>

<!--
	Indicates if pricing information is temporary or final. If set to true at the order item level, pricing for the 
	item is considered temporary and the supplier cannot invoice the line item.
	
	If set to true at the order header level, pricing is considered temporary for the entire purchase order and 
	the supplier cannot invoice against any item on the order.
-->
<!ELEMENT TemporaryPrice EMPTY>
<!-- 
	@attr value If set to "yes" at the order item level, pricing for the item is considered temporary and the 
		supplier cannot invoice the line item. If set to "yes" at the order header level, pricing is considered 
		temporary for the entire purchase order and the supplier cannot invoice against any item on the order. 
-->
<!ATTLIST TemporaryPrice value (yes | no) #REQUIRED
>

<!--
	Indicates whether a service sheet is allowed for this order or line item, regardless of the default business 
	rules configured in the network hub.

	Lower: Specifies tolerances that define a lower limit.
	
	Upper: Specifies tolerances that define an upper limit.
-->
<!ELEMENT SESInstruction (Lower?, Upper?)>
<!--
	@attr value Value that indicates whether a service sheet is allowed. Possible values:
		<ul>
		<li>allowed - Service sheet is allowed.</li>
		<li>notAllowed - Service sheet is not allowed.</li>
		</ul>

	@attr unitPriceEditable Specifies whether a buyer or supplier is allowed to update the unit 
		price while creating a service sheet. Possible values:
		<ul>
		<li>yes - Buyer or supplier is allowed to edit unit price. The new price must be within price tolerance.</li>
		<li>no - The unit price is not editable regardless of transaction rules defined in the network hub.</li>
		</ul>

		If the unitPriceEditable attribute is not present, the system honors the default transaction rules present 
		in the network hub.
-->
<!ATTLIST SESInstruction
    value       (allowed | notAllowed)  #REQUIRED
    unitPriceEditable (yes | no)  	#IMPLIED>

<!--
    Either or both a textual description and codes about a hazard inherent
    in an item or for the overall order or shipment. The overall hazards
    may be due either to identical hazards for all items or hazards
    inherent in shipping various products together.

    The contained Description element list (if provided) should include
    detailed handling requirements. Elements in this list may appear in
    any order. A description locale (Description&#064;xml:lang attribute value)
    must not appear more than once in a Hazard element. When more than one
    Description element is present, all must contain translations of a
    common description.

    Classification elements may appear in any order. A classification
    domain (Classification&#064;domain attribute value) must not appear more
    than once in a Hazard element.

    All listed Classification elements and the Description (if provided)
    must relate to a single hazard. Additional hazards must use separate
    Hazard elements.

    The following Classification domains are expected in this context:
	<ul>
	<li>UNDG - United Nations Dangerous Goods</li>
    <li>IMDG - International Marine Organization Dangerous Goods</li>
    <li>NAHG - North American Hazardous Goods</li>
	</ul>
-->
<!ELEMENT Hazard ((Description+, Classification*) | Classification+)>

<!--
	Number of days the buyer schedules for the inspection of goods after receiving them.
-->
<!ELEMENT PlannedAcceptanceDays  (#PCDATA)> <!-- number -->

<!--
    Defines one or more authorization labels for the document.
    
    A authorization label identifies a document as part of a certain, user-defined, classification
    but doesn't by itself grant or deny access to a user. Instead, it's up to the application
    to decide if a user should or shouldn't have access based on this info.
    
    Each IdReference means a different domain. No duplicate domains are allowed. Multiple
    values within the same domain must appear as comma-separated values in the same element.
    
    Examples of valid domains are:
    <li>sip : In Aerospace and defense industries, SIP labels or SIP tags most commonly refer to durable, unique identification labels used for asset tracking and compliance with military standards. They are essential for efficient asset management, regulatory compliance, and operational readiness.<li>
    <li>program: A program in the aerospace and defense industry is a coordinated set of related projects, activities, and resources managed together to achieve a significant, strategic outcome—typically the development, acquisition, or sustainment of complex systems such as aircraft, spacecraft, missiles, satellites, or defense platform. </li>
    <li>contract: A prime contract in aerospace and defense is a direct agreement between a company and a government agency for the delivery of major defense or aerospace products and services. The prime contractor is fully responsible for contract execution and compliance, often coordinating a network of subcontractors to fulfill the contract’s requirements. </li>
    <li>documentVisibility: This domain is used to specify the visibility of the documents.</li>
    for documentVisibility:
    	- identifier="citizen": Document should only be available to users that are US citizens.
		- identifier="US-Resident": Document should only be available to users that are lawful permanent residents (i.e. Green Card holders).
-->
<!ELEMENT AuthorizationLabels (IdReference+)>

<!--
   The domain of the authorization label. This is a unique identifier for the
   domain of the label. The value of this attribute must be unique within the
   document.

   DPAS: Header-level DPAS-rating info for this document, if applicable.
   AuthorizationLabels: ABAC labels for configuring access to this document.
-->
<!ELEMENT AerospaceAndDefense (AuthorizationLabels?, DPAS?)>

<!--
    @attr acknowledgementRequired    The attribute ‘acknowledgementRequired’ allows orders to send the new flag explicitly. This new attribute is relevant for Orders only.
	Acknowledgement is a process to ‘Accept’ or ‘Reject’ the terms and conditions of the Defense Procurement Order. This attribute is required for DPAS-rated orders. However, it is optional for non-DPAS-rated orders.
	The ‘acknowledgementRequired’ attribute’s default value is "yes".
	If the value is set to "yes", the order acknowledgement status will be set to "ActionRequired".
	If the value is set to "no", the order acknowledgement status will be set to "NotRequired".
-->
<!ATTLIST AerospaceAndDefense
    acknowledgementRequired	(yes | no)	#IMPLIED
>

<!--
    Indicates that the order is DPAS-rated. This requires a special handling
    to comply with the applicable law.

    This element is only applicable in the US.
-->
<!ELEMENT DPAS EMPTY>
<!--
    @attr rating    The DPAS rating, such as DO or DX. Even more specific ratings
        can be put here.
    @attr contract  The contract number related with this DPAS-rated order.
        Shouldn't be present for header-level.
    @attr quantity  The allocation quantity, for line-level or lower information.
        Shouldn't be present for header-level.
-->
<!ATTLIST DPAS
    rating      %string;    #REQUIRED
    contract    %string;    #IMPLIED
    quantity    %r8;        #IMPLIED
>

