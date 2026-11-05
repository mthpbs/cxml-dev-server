<!--
    For cXML license agreement information, please see
    http://www.cxml.org/home/license.asp

    $Id$
-->

<!--
    This file defines the basic elements used to build higher level
    constructs in cXML.
-->

<!-- Basic Name/Data Elements -->
<!--
    Name is used to provide an identifier for other elements.
-->
<!ELEMENT Name (#PCDATA)> <!-- string -->
<!--
	@attr xml:lang The language in which the name is written. 
-->
<!ATTLIST Name
    xml:lang  %xmlLangCode;  #REQUIRED
>

<!--
    An Extrinsic is an element which can be used to extend the data
    associated with known elements.

    Since this Element is of type ANY, it could contain any arbitrary XML
    document within itself, or a binary ![CDATA[]] document.
-->
<!ELEMENT Extrinsic ANY>
<!-- 
	@attr name Name used to identify this extrinsic. 
-->
<!ATTLIST Extrinsic
    name  %string;  #REQUIRED
>

<!--
    Description is a string which describes something.
    Though text may be interspersed with ShortName elements in this content
    model, placing the ShortName at the beginning or end of the element is
    much preferred. At most one ShortName element is allowed per
    Description. The intended content model would be more like
    (( ShortName, #PCDATA ) | ( #PCDATA | ShortName? )) if DTD syntax
    supported it.
-->
<!ELEMENT Description ( #PCDATA | ShortName )* > <!-- mixed: string and
                                                      ShortName -->
<!-- 
	@attr xml:lang The language in which the description is written.

    @attr type The qualifier of the description. 
-->
<!ATTLIST Description
    xml:lang  %xmlLangCode;  #REQUIRED
    type  %string;   #IMPLIED
>

<!--
    A short string which describes something in fewer characters than the
    entire Description. This should be used when limited space is available.
    For example, a table of elements might show the ShortName's of each. A
    linked "details" view would show the entire Description (including the
    ShortName). Without a ShortName, the user interface must default to a
    truncation of the Description.
	
    This element does not require an xml:lang attribute since it appears only
    within a Description element. The language of the ShortName must match
    that of the surrounding Description.
-->
<!ELEMENT ShortName (#PCDATA)> <!-- string -->

<!-- Telephone Number Elements -->
<!--
    International ITU dial code for the country code in question.  This
    code would be entered after any escape code necessary to begin
    International dialing. That is, the escape code does not appear in the
    content of this element.
-->
<!ELEMENT CountryCode (#PCDATA)> <!-- uint -->
<!-- 
	@attr isoCountryCode The ISO 3166 2-letter country code for the dial code in question.  
-->
<!ATTLIST CountryCode
    isoCountryCode  %isoCountryCode;  #REQUIRED
>

<!--
    The area code or city code within a CountryCode.
-->
<!ELEMENT AreaOrCityCode (#PCDATA)> <!-- uint -->

<!--
    The local number part of a telephone number.
-->
<!ELEMENT Number (#PCDATA)> <!-- string -->

<!--
    An extension within relative to the Number element. This element has no
    meaning without an associated Number element.
-->
<!ELEMENT Extension (#PCDATA)> <!-- uint -->

<!--
    TelephoneNumber represents international telephone numbers.
-->
<!ELEMENT TelephoneNumber (CountryCode, AreaOrCityCode, Number, Extension?)>

<!--
     Phone is a "named" TelephoneNumber.
-->
<!ELEMENT Phone (TelephoneNumber)>
<!-- 
	@attr name Specifies an identifier which indicates the type of phone number. US examples would include 
		"work","home", and so on. 
-->
<!ATTLIST Phone
    name  %string;  #IMPLIED
>

<!--
    Fax number.
-->
<!ELEMENT Fax (TelephoneNumber | URL | Email)>
<!-- 
	@attr name Name given to the fax number. 
-->
<!ATTLIST Fax
    name  %string;  #IMPLIED
>

<!-- Addressing Elements -->
<!--
    A string that represents a URL.
-->
<!ELEMENT URL (#PCDATA)> <!-- URL -->
<!-- 
	@attr name Name given to the URL. 
-->
<!ATTLIST URL
    name  %string;  #IMPLIED
>

<!--
    An email address. Address must conform to RFC 821 (SMTP Standard).
-->
<!ELEMENT Email (#PCDATA)> <!-- string -->
<!-- 
	@attr name Name given to the email address.

    @attr preferredLang Optional language that the email owner prefers to receive emails in. Refer to the definition 
		of the xmlLangCode entity. 
-->
<!ATTLIST Email
    name          %string;       #IMPLIED
    preferredLang %xmlLangCode;  #IMPLIED
>

<!--
    Contact represents an entity at a location. The nature of this
    element is that it represents a communication "end point" for a
    location.

	IdReference: ID reference is Within the application context (for example,
       a certain pair of buyer and supplier), the (identifier, domain) 
	   pair should be unique.
-->
<!ELEMENT Contact (Name, PostalAddress*, Email*, Phone*, Fax*, URL*, IdReference*, Extrinsic*)>
<!-- 
	@attr role Position this person or group plays in the procurement process.
        Likely values include endUser, administrator, purchasingAgent,
        technicalSupport, customerService, sales, supplierCorporate, supplierMasterAccount, 
		supplierAccount, buyerCorporate, buyerMasterAccount, buyerAccount, buyer,
        subsequentBuyer. Other values may be allowed in some cases.
        The from and to roles are reserved for future use.

    @attr addressID An id for the address. Needed to support address codes for relationships that require id references. Example: plantID

    @attr addressIDDomain The qualifier for the addressId to specify the agency or organization. Examples are DUNS, ILN, and buyerLocationID.
-->
<!ATTLIST Contact
    role             NMTOKEN           #IMPLIED
    addressID        %string;          #IMPLIED
    addressIDDomain   %string;   #IMPLIED
>

<!--
    The DeliverTo part of an Address. This would be internal to the actual
    address know to the outside world. Similar to what an extension is to a
    TelephoneNumber.
-->
<!ELEMENT DeliverTo (#PCDATA)> <!-- string -->

<!--
    Street is a single line of an Address's location.
-->
<!ELEMENT Street (#PCDATA)> <!-- string -->

<!--
    City is the name of the city in an Address's location.
-->
<!ELEMENT City (#PCDATA)> <!-- string -->
<!-- 
	@attr cityCode Optional code assigned by local/government agencies identifying a city region. 
-->
<!ATTLIST City
    cityCode  %string;  #IMPLIED
>

<!--
    State is an optional state identifier in the location for an Address.
-->
<!ELEMENT State (#PCDATA)> <!-- string -->
<!-- 
	@attr isoStateCode The ISO 3166-2:2013 country subdivision (state) code for this country.
        Universally applicable code for the representation of the names of
        principal administrative divisions of countries and territories included
        in ISO 3166-1. It is intended to be used in conjunction with ISO 3166-1. 
-->
<!ATTLIST State
    isoStateCode  %isoStateCode;  #IMPLIED
>

<!--
    A series of letters or digits or both, sometimes including spaces or punctuation, included 
	in a postal address for the purpose of sorting mail. In the United States, postal code 
	is called "ZIP code."
-->
<!ELEMENT PostalCode (#PCDATA)> <!-- string -->

<!--
    Country is the name of the country in an Address's location. The
    content of this element is a string which may (for example) be printed
    directly to a shipping label. The content is the human-readable
    equivalent of the isoCountryCode used by applications.
-->
<!ELEMENT Country (#PCDATA)> <!-- string -->
<!-- 
	@attr isoCountryCode The ISO 3166 2-letter country code for this country. 
-->
<!ATTLIST Country
    isoCountryCode  %isoCountryCode;  #REQUIRED
>

<!--
    Municipality specifies the name of the municipality in the location for an Address.
-->
<!ELEMENT Municipality (#PCDATA)> <!-- string -->
<!-- 
	@attr municipalityCode Optional code assigned by local/government agencies identifying a municipality region. 
-->
<!ATTLIST Municipality
    municipalityCode  %string;  #IMPLIED
>


<!--
    PostalAddress is a real-world location for a business or person.
-->
<!ELEMENT PostalAddress (DeliverTo*, Street+, City, Municipality?, State?,
                         PostalCode?, Country, Extrinsic*)>
<!-- 
	@attr name Name given to the postal address. 
-->
<!ATTLIST PostalAddress
    name  %string;  #IMPLIED
>

<!--
    Address is the association of a Contact and a Location.
-->
<!ELEMENT Address (Name, PostalAddress?, Email?, Phone?, Fax?, URL?)>
<!-- 
	@attr isoCountryCode The ISO 3166 2-letter country code for the country containing this location.

    @attr addressID An id for the address. Needed to support address codes for
        relationships that require id references. An example would be a shipping code.

    @attr addressIDDomain It is the qualifier for the addressId to specify the agency or organization. 
		Examples are DUNS and ILN.
-->
<!ATTLIST Address
    isoCountryCode  %isoCountryCode;  #IMPLIED
    addressID       %string;          #IMPLIED
    addressIDDomain   %string;   #IMPLIED
>

<!--
	Defines the parties involved in a document (for example, service entry sheet), including 
	fieldContractor, fieldEngineer, and requester.

	Contact: Specifies contact information for the document partners. Allowed 
		contact roles include fieldContractor, fieldEngineer, and requester.

	IdReference: Partner reference ID, supplier tax ID, and so on.
-->
<!ELEMENT PartnerContact (Contact, IdReference*)>

<!--
    Defines the information of the issuer who is associated to the customs info document.
	
	Address: Defines the real-world location of the issuer.
-->
<!ELEMENT Issuer ( Address )>

<!-- Financial Elements -->

<!--
	Money is the representation of the object used to pay for items.
-->
<!ELEMENT Money (#PCDATA)> <!-- number -->
<!-- 
	@attr currency Specifies the currency in which amount is stated, must conform to ISO 4217 currency codes.

	@attr alternateAmount The amount of money in the alternateCurrency. Optional and used to support dual-currency requirements such as the Euro.

	@attr alternateCurrency Specifies the currency in which the alternateAmount is stated. Must conform to ISO 4217 currency codes. 
-->
<!ATTLIST Money
    currency           %isoCurrencyCode;  #REQUIRED
    alternateAmount    %number;           #IMPLIED
    alternateCurrency  %isoCurrencyCode;  #IMPLIED
>

<!--
	Optional textual child for communicating arbitrary comments or description along with the parent.

	Text may be interspersed with Attachment elements in this content model, grouping the Attachment 
	list at the begging or end of the element is much preferred. The intended content model would 
	be more like 
	%
	(( Attachment+, #PCDATA ) | ( #PCDATA | Attachment* ))
	%
	if the DTD syntax supported it.
-->
<!ELEMENT Comments ( #PCDATA | Attachment )* > <!-- mixed: string and
                                                    opt. Attachment list -->
<!-- 
	@attr xml:lang The language in which the Comments are written. This attribute will be required in 
		a future version of cXML. (Leaving it out is deprecated.)

	@attr type An optional type of the comments. Type can be used to make the comments structured with 
		respect to what they describe. 
-->
<!ATTLIST Comments
    xml:lang  %xmlLangCode;  #IMPLIED
    type      %string;       #IMPLIED
>

<!--
    Optional child of Comments element referencing a part in a multipart MIME
    transmission.

    The contained URL must use the scheme "cid:". This is the identifier for
    the referenced attachment within the larger transmission. Must match the
    Content-ID header of one (and only one) part of the MIME transmission
    containing this cXML document. May also be used to retrieve the
    attachment file separately.
-->
<!ELEMENT Attachment (URL)>
<!-- 
	@attr visibility Optional attribute that gives a hint as to how an attachment should be interpreted. 
		If the value is "internal”, it will give a hint to the consumer of the attachment, that the attachment 
		is meant for internal purposes only. 
-->
<!ATTLIST Attachment
    visibility (internal)  #IMPLIED
>

<!---
    Reference to a remote attachment.

    AttachmentReference is used inside Extrinsic elements that have a
    predefined name of "Attachments".

    In the context of AttachmentReference, the domain attribute of
    InternalID is currently optional. However, as a way to prevent
    circular request paths, the sending application may use a
    predefined value of "local" to indicate that the attachment
    requested is local to the other application.

    This optional URL element should link to a referenced source on the buyer’s
    system. The URL scheme must conform to RFC 1738 (Uniform Resource Locators).
    Both https and http transmission protocols are supported, however due to
    security reasons we highly recommend to use https with the latest TLS set
    only.
-->
<!ELEMENT AttachmentReference (Name, Description, InternalID, URL?)>
<!-- 
	@attr length Length of the attachment in bytes.
        
    @attr version Specifies the version of the external source. Examples: 1.0, beta, V-2. 
-->           
<!ATTLIST AttachmentReference
    length  %uint;  #IMPLIED
    version     %string; #IMPLIED 
>

<!---
    Reference to a pricing condition cost term .

    Cost term can be of money or percentage type                
-->

<!ELEMENT CostTermValue (Money|Percentage)>

<!---
    Reference to a pricing condition type.

    This represents the pricing condition type. ConditionType can have default 
    cost term value, which will be used in cases where scales are not specified. 
    If the scale is specified, the cost term value provided in the scale will be used.
-->

<!ELEMENT ConditionType (CostTermValue?,Scales?)>
<!--
     @attr name Name of the condition type. Example: Price, Surcharge.
-->
<!ATTLIST ConditionType
    name  %string; #REQUIRED 
>

<!---
    This represents the volume scale and there can be more than one 
    cost term per scale. The scale type is specified in the Scales element.

    The value which is specified for a scale is always in the context of a scale type which is selected. 
 
    The attributes for a scale depends on the scale type. If the scale type is selected as "From", then
    the scale should have "from" value. If the scale type is "To" scale, then the scale should have the 
	value for "to" attribute.

    If the scale is of type "Graduated", then both the "from" and "to" values should be provided.

    Example 1: From scale
	%
	<Scale from='0'> 
	<Scale from='1000'>
	<Scale from='5000'>
	%
    In the above example, there are three scales: From 0, From 1000, and From 5000. 
    
    Example 2: To scale
	%
	<Scale to='100'> 
	<Scale to='1000'>
	<Scale to='5000'>
	%
    In the above example, there are three scales: To 100, To 1000, and To 5000. 

    Example 3: Graduated scale
	%
	<Scale from='0' to='100'>
	<Scale from='101' to='200'>
    %
	In the above example, there are two scales: From 0 To 100 and From 101 To 200.
-->
<!ELEMENT Scale (CostTermValue+)>
<!--
    @attr from This represents the lower bound value for the scale.
    @attr to   This represents the upper bound value for the scale.
-->
<!ATTLIST Scale
    from   %number; #IMPLIED 
    to     %number; #IMPLIED 
>

<!---
    Reference to a pricing condition scales vector.

    This represents the volume scales list for a given validity period.
-->
<!ELEMENT Scales (Scale+)>
<!--
    @attr scaleType This represents the scale type for the validity period. The value for this attribute can be
        "From", "To", or "Graduated". Please refer to the examples provided in scale element for more information.

    @attr scaleBasis This represents the scale basis.
-->
<!ATTLIST Scales
    scaleType  (From|To|Graduated) #REQUIRED
    scaleBasis %string; #FIXED 'quantity'
>

<!---
    Reference to a condition types list.               
-->
<!ELEMENT ConditionTypes (ConditionType+)>

<!---
    Reference to a pricing condition validity period.

    This represents the validity period of the price condition with start date and end date.
    The validity period can have list of condition types.
-->
<!ELEMENT ValidityPeriod (ConditionTypes)>
<!--
    @attr from Start date of the validity period.

    @attr to End date of the validity period.
-->
<!ATTLIST ValidityPeriod
    from      %datetime.tz;         #REQUIRED
    to        %datetime.tz;         #REQUIRED
>

<!---
    Reference to list of validity periods. The list should have 
    at least one validity period. 
-->
<!ELEMENT ValidityPeriods (ValidityPeriod+)>

<!---
    Reference to pricing condition object for a given line item.
    The cost elements are added to pricing conditions per validity period
    and scale. If any cost term like Discount, Surcharge is added to 
    pricing condition, then it will not be part of modifications.     
-->
<!ELEMENT PricingConditions (ValidityPeriods)>

<!--
    Price per unit of item.

    Money: Contains the final amount after applying Modifications.

	Modifications: Contains details of the allowances and charges applicable 
	at the line-item level.
    
	PricingConditions: Pricing condition that represents the cost elements with
		validity period and optional scales.
-->
<!ELEMENT UnitPrice (Money, Modifications?, PricingConditions?)>

<!--
    Reference to an earlier document (for example, OrderRequest). In a
    StatusUpdateRequest, this element identifies the purchase order to be
    updated.
-->
<!ELEMENT DocumentReference EMPTY>
<!-- 
	@attr payloadID A unique identifier for the document. Copied directly from the
        cXML element of the original document. 
-->
<!ATTLIST DocumentReference
    payloadID       %string;      #REQUIRED
>

<!--
	Contains a unique internal ID for a document.
-->
<!ELEMENT InternalID (#PCDATA)> <!-- string -->
<!--
	@attr domain A string indicating the type of internal ID.
-->
<!ATTLIST InternalID
    domain   %string;  #IMPLIED
>

<!-- ====
    Common to most variants of the PunchOut transaction set. Defined here
    to be easily shared between multiple DTD files without requiring
    inclusion of Transaction.mod in all of them.

    All of the PunchOut transaction sets include an originating Request
    (ProviderSetupRequest for example), relatively simple Response
    (PunchOutSetupResponse for example) and final Message
    (ProviderDoneMessage or PunchOutOrderMessage). The Request and
    Response comprise a back-end transaction between two cooperating
    applications that wish to extend an interactive session from one to the
    other. The Request provides the destination application with
    authentication, identification and other setup information. The
    Response provides the originating application with a unique starting
    location for the interactive (HTML) session at the destination system.

    After receiving a Response of this type, the originating application
    redirects the user's browser to the provided location. (For some
    non-HTML applications, opening a new browser window at that location
    may be more appropriate.) The destination system eventually provides
    an HTML form to the user's browser. This form submits the final
    Message to close the remote session, return that user to the
    originating application and carry any required information back to the
    originating application.
==== -->

<!--
    Identification of a specific PunchOut session. Used in both originating 
	Request and later Message that returns user to originating application.

    Note: The BuyerCookie element used in a 'regular' PunchOut transaction
    (defined in Transaction.mod) is of type ANY. That does not seem
    useful. The string required below better matches the needs for this
    element. Future transactions similar to the PunchOut transaction will
    use this element.
-->
<!ELEMENT OriginatorCookie (#PCDATA)>

<!--
    Location to which the user's browser must submit the final Message. 
	This location (carried in the originating Request) does not need to 
	be specific to a PunchOut session since the OriginatorCookie is 
	returned in the Message.
-->
<!ELEMENT BrowserFormPost (URL)>

<!--
    Identification of a service offered by this provider and requested in 
	this transaction. Used only in the originating Request.
-->
<!ELEMENT SelectedService (#PCDATA)>

<!--
    Location to which the user's browser must be redirected to
    begin the interactive portion of the session at the remote site. The
    destination system returns this information in the Response document.
    This location must be specific to a particular session. It is
    effectively a one time key, providing authenticated entry into the
    destination system.
-->
<!ELEMENT StartPage (URL)>

<!--
    Any information the originator must know about the completed operation 
	at the provider site. The ReturnValue is for applications; the Name is 
	for human consumption (direct presentation in the User Interface of the 
	application). Where appropriate for the possible services, this element 
	may appear in the final Message for a PunchOut session.
-->
<!ELEMENT ReturnData (ReturnValue, Name)>
<!-- 
	@attr name An identifier for the data returned. Provides a meaning for the
        contents of a ReturnData element. 
-->
<!ATTLIST ReturnData
    name  %string;  #IMPLIED
>

<!--
	A value that is used by the originating application. This value depends on 
	what service the provider supplies.
-->
<!ELEMENT ReturnValue (#PCDATA)>

<!--
    Defines a time range. The start and end can be unbounded.
-->
<!ELEMENT TimeRange EMPTY>
<!-- 
	@attr startDate The starting date of the time range.

    @attr endDate The ending date of the range. 
-->
<!ATTLIST TimeRange
    startDate      %datetime.tz;         #IMPLIED
    endDate        %datetime.tz;         #IMPLIED
>

<!--
    Defines a period in time.
-->
<!ELEMENT Period EMPTY>
<!-- 
	@attr startDate The starting date of the period.

    @attr endDate The ending date of the period. 
-->
<!ATTLIST Period
    startDate      %datetime.tz;         #REQUIRED
    endDate        %datetime.tz;         #REQUIRED
>

<!--
    Describes how the product is packaged or shipped. It must conform with UN/CEFACT 
	(Recommendation 20) Unit of Measure Common Codes.
-->
<!ELEMENT UnitOfMeasure (#PCDATA)> <!-- nmtoken -->


<!--
    Defines the quantity as quoted in the UnitPrice.
	
	UnitOfMeasure: The unit of measure for the quote.
	
    Description: The textual description of the quote.
-->
<!ELEMENT PriceBasisQuantity (UnitOfMeasure, Description?)>
<!-- 
    @attr quantity The quantity basis for the quote.

    @attr conversionFactor The conversion ratio from the unit of measure of the order to 
		the unit of measure of the PriceBasisQuantity. 
-->
<!ATTLIST PriceBasisQuantity
    quantity         %r8;    #REQUIRED
    conversionFactor %r8;    #REQUIRED
>

<!--
    Defines a reference to a term which is defined in another document.
-->
<!ELEMENT TermReference EMPTY>
<!-- 
	@attr termName The name of the ID attribute containing the term.

    @attr term The value of that attribute (that is, the term itself). 
-->
<!ATTLIST TermReference
	termName %string; #REQUIRED
	term %string; #REQUIRED
>

<!--
    Defines an optionally named monetary rate at which goods or services are
    charged or paid.
	
    Money: The amount of Money per UnitOfMeasure to be charged or paid.

    UnitOfMeasure: Unit of measure.

    PriceBasisQuantity: The quantity the price is quoted on

    TermReference: Identifies the definition of this UnitRate (found, for example, 
		in contracts, master agreements, and other documents which may or may not be cXML 
		documents).	
-->
<!ELEMENT UnitRate (
    Money,
    UnitOfMeasure,
    PriceBasisQuantity?,
    TermReference?
)>

<!--
    The rate information used to define the rate of a purchase. For
    example, this could be the total room rate for 4 nights of stay
    at a hotel.
	
    Total: The total amount for the rate. The total amount must equal
		to quantity x UnitRate.

    UnitRate: The single unit rate.

    Description: Textual description for the rate. For example, hotel nightly rate.
-->
<!ELEMENT Rate (Total, UnitRate, Description?)>
<!--
    @attr quantity The quantity associated with the rate. For example, a 4-night stay at a hotel will
		have quantity set to 4 with UnitofMesure in UnitRate set to Day. 
-->
<!ATTLIST Rate
    quantity    %r8;    #REQUIRED
>

<!--
    The total for something.
-->
<!ELEMENT Total (Money, Modifications?)>

<!--
    Tolerances for price, quantity, and delivery date.
-->
<!ELEMENT Tolerances (QuantityTolerance?, PriceTolerance?, TimeTolerance?)>

<!--
	Contains the quantity tolerance for a line item.
-->
<!ELEMENT QuantityTolerance (Percentage | Value)>

<!--
	Contains the price tolerance for a line item.
-->
<!ELEMENT PriceTolerance (Percentage | Money)>

<!--
    Defines a certain amount of days used to check if a concrete
    delivery date is within the tolerance regarding the requested
    delivery date.
-->
<!ELEMENT TimeTolerance EMPTY>
<!-- 
	@attr limit Amount of time in 'type' relative to the requested delivery date is it allowed to deliver.

    @attr type Specifies how the limit should be interpreted. 
-->
<!ATTLIST TimeTolerance
    limit %int; #REQUIRED
    type  (minutes | hours | days | weeks) "days"
>

<!-- 
	Upper tolerances define an upper limit 
-->
<!ELEMENT Upper (Tolerances)>
<!-- 
	Lower tolerances define a lower limit 
-->
<!ELEMENT Lower (Tolerances)>
<!--
	The amount of tax.
-->
<!ELEMENT TaxAmount (Money)>
<!--
	The amount that is taxable.
-->
<!ELEMENT TaxableAmount (Money)>

<!--
    One language-specific string for the location of tax,
    for example, London, Canada, California, and so on.
-->
<!ELEMENT TaxLocation (#PCDATA)>              <!-- string -->
<!-- 
	@attr xml:lang The language or locale in which the location of tax is written. 
-->
<!ATTLIST TaxLocation
   xml:lang              %xmlLangCode;         #REQUIRED
>

<!--
    TriangularTransactionLawReference indicates the relevant law as
    titled for the local jurisdiction in the scenario of a triangular
    transaction, for example, Triangulation, article 28c,E paragraph 3 
	of the 6th EU VAT Directive.
-->
<!ELEMENT TriangularTransactionLawReference (#PCDATA)>
<!-- 
	@attr xml:lang The language in which the law reference is written. 
-->
<!ATTLIST TriangularTransactionLawReference
    xml:lang   %xmlLangCode;    #REQUIRED
>

<!--
    If you claim an exemption from taxes, some tax authorities, such as 
	Portugal SAF-T, require you to specify an exemption code and exemption reason.
-->
<!ELEMENT TaxExemption (ExemptReason?)>
<!-- 
	@attr exemptCode Specifies the code against which the tax is exempted. 
-->
<!ATTLIST TaxExemption
    exemptCode    %string;      #REQUIRED 
>

<!--
    Specifies the exemption reason for zero tax.
		
	This would have no relation with the existing "exemptDetail" attribute. Existing attribute 
	"exemptDetail" will not be ignored and this will also will be kept along with the new 
	element "ExemptReason".
-->
<!ELEMENT  ExemptReason  (#PCDATA) >
<!-- 
    @attr xml:lang The language in which the exemption reason is written. 
-->				
<!ATTLIST  ExemptReason
    xml:lang   %xmlLangCode;    #REQUIRED 
>

<!--
    Defines details of one type of tax.
	
    TaxableAmount: The taxable amount.

    TaxAmount: The tax amount.

    TaxLocation: The tax location.

    TaxAdjustmentAmount: This is the adjustment amount for the tax for the specific tax 
		line (tax for a specific category and location) or in other words the adjustment
        amount corresponding to tax represented by this TaxDetail element and
        is optional. Currently it is ONLY supported for the PPR(PaymentProosalRequest)
        document and also ONLY applicable for the PPR document.
		
        Note: Also this is to be used instead of TaxAdjustmentDetail(now deprecated)
        element in the PPR document.

    Description: The textual description of the current type of tax.

    TriangularTransactionLawReference: The law reference for transactions where isTriangularTransaction 
		is True.
-->
<!ELEMENT TaxDetail (TaxableAmount?, TaxAmount, TaxLocation?, TaxAdjustmentAmount?, Description?,
                     TriangularTransactionLawReference?, TaxRegime?, TaxExemption?,
					 Extrinsic*)>
<!-- 
    @attr taxedElement An ID reference that uniquely identifies the item being taxed???

    @attr purpose The purpose of the tax, for example, tax (tax), custom duty, shippingTax,
       specialHandlingTax, and so on.

    @attr category The tax category, Sales tax (sales), Use tax (usage), VAT (vat),
       GST (gst) are defined categories. Other values are permitted.

    @attr percentageRate The tax rate in number of percentage.

    @attr isVatRecoverable Set to "yes" if the entire invoice is VAT (Value Added Tax)-recoverable. Default is false.

    @attr taxPointDate Refers to the date on which VAT becomes due.

    @attr paymentDate Indicates the date when payment must be made.

    @attr isTriangularTransaction Set to "yes" to indicate that the transaction occurred between 
		three parties in three different countries, but the movement of goods did not follow the 
		invoicing route. Add a Contract element with role="subsequentBuyer" to identify the 
		subsequent buying organization in triangular transactions.

    @attr exemptDetail If the TaxAmount is zero, VAT or other regulations may require
		a reason why. Possible values:
		<ul>
		<li>zeroRated - The tax rate is zero.</li>
		<li>exempt - The item or items are exempt from taxation.</li>
		</ul>
	   
	@attr isWithholdingTax Set to "yes" if the tax is a withholding tax. 
	   
    @attr taxRateType Specify the tax rate type code (a string) that corresponds to a specific tax type. 
 	   For example, in Germany the tax rate of 19\% for value-added tax (VAT) corresponds to the "Standard" tax rate type. 
 	   In different EU member states, different VAT rates are allowed.
	   
 	   The tax rate type makes it easy for the back-end system to handle different rates that result from changes in tax law.

    @attr basePercentageRate The base tax rate in number of percentage. For some Tax categories (for example, ICMS for 
		Brazil) TaxAmount is calculated considering the basePercentageRate along with percentageRate.
		%
		TaxAmount = TaxableAmount * percentageRate * basePercentageRate
		$
		Used only in the Quote Messages.

    @attr isIncludedInPrice Set to "yes" (true) if the TaxAmount is included in the price. The default is "no" (false). 
		Used only in Quote messages.
-->
<!ATTLIST TaxDetail
    taxedElement            IDREF          #IMPLIED
	purpose                 %string;       #IMPLIED
    category                %string;       #REQUIRED
    percentageRate          %r8;           #IMPLIED
    isVatRecoverable        (yes)          #IMPLIED
    taxPointDate            %datetime.tz;  #IMPLIED
    paymentDate             %datetime.tz;  #IMPLIED
    isTriangularTransaction (yes)          #IMPLIED
    exemptDetail            (zeroRated | exempt)  #IMPLIED
	isWithholdingTax        (yes)          #IMPLIED
	taxRateType				%string;	   #IMPLIED
    basePercentageRate      %r8;           #IMPLIED
    isIncludedInPrice       (yes)          #IMPLIED
>

<!--   
    TaxRegime is related to the type of supplier activities and the
	commodities in the invoice. Companies pay taxes based on the tax regime
	they are enrolled. Sample tax regimes: Assimilated Regime wages, Regime
	of Small Taxpayers (REPECO), Regimen of business and professional activities,
	etc. Multiple tax regimes can be associated with a single invoice.
-->
<!ELEMENT TaxRegime (#PCDATA)>


<!--
    Definition of a cXML Tax item. This represents what a Tax element should
    be in the classic notion of a line on a PO or Invoice. It can also
    represent a per-line-item tax element depending on where it appears
    (inside of a item ELEMENT or inside of a something like a supplierOrder
    ELEMENT).

    Represents a tax item in the shopping basket. There could be one of these
    for the entire order, or one per line item.

    The total amount of taxes for the line item or order should be reflected
    in the Money element. The breakdown of taxes, for example, tax on shipping
    or tax on goods should be represented in separate TaxDetail elements.

    TaxAdjustmentAmount: The adjustment amount for the total tax for the document 
		or in other words the adjustment amount corresponding to tax represented 
		by this Tax element and is optional (this is supposedly the sum of the 
		amounts in TaxAdjustmentAmount elements under the TaxDetail children of 
		this Tax element). Currently, this element is ONLY supported for the 
		PaymentProposalRequest document. It is used instead of the TaxAdjustment 
		element (now deprecated).

-->
<!ELEMENT Tax (Money, TaxAdjustmentAmount?, Description, TaxDetail*, Distribution*, Extrinsic*)>

<!--
    Defines a Modifications element which provides the original unit
    price or shipping price and then describes a set of 1 or more
    modifications to that price. There can one per Shipping element.
    The optional method attribute defines how multiple Modification elements
    are combined.
-->
<!ELEMENT Modifications (Modification+)>

<!--
    Defines the original price or shipping price and optionally provides a type.    
-->
<!ELEMENT OriginalPrice (Money)>
<!-- 
    @attr type Type of price. Examples include MSRP, ListPrice, Actual, AverageSellingPrice,
      CalculationGross, BaseCharge, AverageWholesalePrice, ExportPrice, AlternatePrice,
      ContractPrice, and so on. 
-->
<!ATTLIST OriginalPrice
    type            %string;  #IMPLIED
>

<!--
    Stores any modification to the original price or shipping price of the item. Provides 
	an optional level attribute for cascading modifications. 
	
	AdditionalDeduction: Used only when allowances are involved.

    AdditionalCost: Used when additional charges are involved.
	
    OriginalPrice: Represents the price on which the discount or charges apply.
	
    Tax: Represents the tax on the Modification.
	
	ModificationDetail: The details of any information for the AdditionalDeduction or 
		AdditionalCost element.
-->
<!ELEMENT Modification (OriginalPrice?, (AdditionalDeduction | AdditionalCost), Tax?, ModificationDetail?)>
<!-- 
    @attr level Represents the level of modification, used in cascading modifications.
	   Example:
	   Charge 1 (Level 1): Original Price 10$ Charge: 1$
	   Charge 2 (Level 1): Original Price 10$ Charge: 1$
	   Charge 3 (Level 2): Original Price 8$ Charge: 1$
	   Charge 4 (Level 3): Original Price 7$ Charge: 1$ ...
-->
<!ATTLIST Modification
    level            %uint;  #IMPLIED
>

<!--
    Contains details of the deductions available for the item. Used only when allowances are applicable.
-->
<!ELEMENT AdditionalDeduction (DeductionAmount | DeductionPercent | DeductedPrice)>
<!-- 
	@attr type Type of deduction (for example, discount). 
-->
<!ATTLIST AdditionalDeduction
    type            %string;  #IMPLIED
>

<!-- 
	Contains details of any information for the AdditionalDeduction or AdditionalCost element. 
-->
<!ELEMENT ModificationDetail (Description?, IdReference?, ParentID?, Extrinsic*)>
<!-- 
    @attr name The name of the modification, for example, "Allowance".
    
    @attr startDate The start date of the modification.

    @attr endDate The end date of the modification.

    @attr code The service code for the modification.

    @attr scope The scope for the modification, for example, ‘UnitPrice’, ‘LineItem’.

    @attr calculationType The calculationType for the modification, for example, ‘fixedAmount’, ‘quantity’, ‘percentage’.
-->
<!ATTLIST ModificationDetail
    name             %string;  #REQUIRED
    code             %string;  #IMPLIED
    startDate        %datetime.tz;  #IMPLIED
    endDate          %datetime.tz;  #IMPLIED
    scope            %string;  #IMPLIED
    calculationType  %string;  #IMPLIED
 >

<!--
	Contains the quantity value for a QuantityTolerance.
-->
<!ELEMENT Value EMPTY>
<!--
	@attr value Quantity value for a QuantityTolerance.
-->
<!ATTLIST Value
	value            %number;       #REQUIRED
>

<!--
	Contains the percentage value for a QuantityTolerance.
-->
<!ELEMENT Percentage EMPTY>
<!--
	@attr percent Percentage for a QuantityTolerance.
-->
<!ATTLIST Percentage
 	percent          %r8;           #REQUIRED
>

<!--
	The Money element is the final cost amount for this Additional Cost or Charge
 	DO NOT USE THIS ELEMENT FOR SHIPPING, SPECIAL HANDLING, FREIGHT, and so on.
-->
<!ELEMENT AdditionalCost (Money|Percentage)>

<!--
    Defines discount applied.

	DiscountAmount: The discount expressed as a flat amount with currency.

    DiscountPercent: The discount rate expressed as a percentage.
-->
<!ELEMENT Discount (DiscountPercent | DiscountAmount)>


<!-- 
	Defines the Basis for the Discount. 
-->
<!ELEMENT DiscountBasis (Money)>

<!-- 
	Defines the details of adjustments given over category and region.
    
	Note: This is deprecated in the PaymentProposalRequest document. Please use
    TaxAdjustmentAmount element that is under TaxDetail element instead.
-->
<!ELEMENT TaxAdjustmentDetail (Money)>
<!-- 
	@attr category String describing the tax adjustment category.
	
    @attr region String describing the region where the tax adjustment occurred. 
-->
<!ATTLIST TaxAdjustmentDetail
    category    %string;        #REQUIRED
    region      %string;        #IMPLIED
>

<!--
    Defines TaxAdjustment given on the Total Tax, currently to be used with
    PPR(PaymentProposalRqeuest) document to specify the total adjustments given
    on the Tax for the payable.
	
    Note: This is deprecated in the PaymentProposalRequest document. Please use
    TaxAdjustmentAmount element that is under Tax element instead.

    Money: Describes the Total Tax Adjustment given on the payable.

    TaxAdjustmentDetail: Describes the list of all the Adjustments given on the 
		payable on the basis of tax category and region.
-->
<!ELEMENT TaxAdjustment (Money, TaxAdjustmentDetail*)>

<!--
    Defines the tax adjustment either on the total tax or for a specific tax
    category and region, currently to be used with PPR(PaymentProposalRequest)
    document to specify the adjustments on tax for the payable.
    If it is included under the Tax element then it is used to specify the
    adjustment for total tax in the PPR(PaymentProposalRequest) document.
    If it is included under the TaxDetail element then it specifies the adjustment
    for tax for a specific tax category and region. 

    Money: Describes the adjustment for the tax(either for Tax or TaxDetail) 
		given on the payable.
-->
<!ELEMENT TaxAdjustmentAmount (Money)>

<!--
    Defines the discount rate
-->
<!ELEMENT DiscountPercent EMPTY>
<!-- 
	@attr percent The discount rate expressed as a percentage. A negative discount percent 
		represents a penalty. 
--> 
<!ATTLIST DiscountPercent
	percent          %r8;           #REQUIRED
>

<!--
    Defines the deduction rate
-->
<!ELEMENT DeductionPercent EMPTY>
<!-- 
	@attr percent The deduction rate expressed as a percentage. 
-->
<!ATTLIST DeductionPercent
	percent          %r8;           #REQUIRED
>

<!--
    Provides a clear reference to the line item in a Consignment Movement.
-->
<!ELEMENT ProductMovementItemIDInfo (IdReference*)>
<!-- 
	@attr movementLineNumber The line number of current line item, copied from movement document.

    @attr movementID The id of a consignment movement entry known to the buyer system.

    @attr movementDate The date and time the product was moved from consignment inventory to the buyer inventory. 
-->
<!ATTLIST ProductMovementItemIDInfo
    movementLineNumber        %uint;              #REQUIRED
    movementID                %string;            #REQUIRED
    movementDate              %datetime.tz;       #REQUIRED
>


<!-- 
	Provides a clear reference to the line item in a InvoiceDetailRequest document.
-->
<!ELEMENT InvoiceItemIDInfo (IdReference*)>
<!-- 
	@attr invoiceLineNumber The line number of a line item, copied from InvoiceDetailRequest.

    @attr invoiceID The id of an invoice known to the supplier system.

	@attr invoiceDate The invoice date. 
-->
<!ATTLIST InvoiceItemIDInfo
    invoiceLineNumber        %uint;              #REQUIRED
    invoiceID                %string;            #REQUIRED
    invoiceDate              %datetime.tz;       #IMPLIED
>

<!--
    Quantity of an item that was returned.
-->
<!ELEMENT ReturnQuantity (UnitOfMeasure)>
<!-- 
	@attr quantity Quantity returned. 
-->
<!ATTLIST ReturnQuantity
    quantity        %r8; #REQUIRED
>

<!--
    This a new limit is directly related with Contract/Catalog items. If this limit is in PO, then validation 
	against this will occur for all Contract items. This field is only visible to buyers.
-->
<!ELEMENT MaxContractAmount (Money)>

<!--
    This a new limit is directly related with ad hoc items. If this is limit is in PO, then validation 
	against this will occur for all ad hoc (non-Contract) items. This field is only visible to buyers.
-->
<!ELEMENT MaxAdhocAmount (Money)>
