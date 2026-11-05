<!--
    For cXML license agreement information, please see
    http://www.cxml.org/home/license.asp

    $Id$
-->

<!--
    For more information about these elements/entities, see the 
    cXML Reference Guide and related documents.
-->

<!-- Basic financial elements used throughout -->

<!--
	The amount of deposit or prepayment.
-->
<!ELEMENT DepositAmount (Money)>

<!--
	Defines the subtotal amount.
-->
<!ELEMENT SubtotalAmount (Money)>

<!--
	Defines the special handling amount.
-->
<!ELEMENT SpecialHandlingAmount (Money, Description?, Distribution*)>

<!--
	Defines the shipping amount.
-->
<!ELEMENT ShippingAmount (Money, Distribution*)>

<!--
	Defines the gross amount, before discounts and adjustments have been applied.
-->
<!ELEMENT GrossAmount (Money)>

<!--
	Defines the net amount, the amount remaining after discounts and adjustments have 
	been applied to the gross amount.
-->
<!ELEMENT NetAmount (Money)>

<!--
	Defines the total amount due.
-->
<!ELEMENT DueAmount (Money)>

<!--
	Defines the discount amount.
-->
<!ELEMENT DiscountAmount (Money)>

<!--
	Defines the fixed amount.
-->
<!ELEMENT FixedAmount (Money)>

<!-- 
    Defines gross amount of the fee associated with a purchase in original currency.
    
	Money: The fee amount.
	
    Fee: Different types of individual fees, for example, service provider fee.
-->
<!ELEMENT FeeAmount (Money, Fee*)>

<!-- 
    Defines different types of individual fees.
-->
<!ELEMENT Fee (Money)>
<!--
    @attr type The type of fee.
-->
<!ATTLIST Fee
    type     %string;  #IMPLIED
>

<!--
	Defines the deduction amount available for an item.
-->
<!ELEMENT DeductionAmount (Money)>

<!--
	Contains the final price of the item. This price overrides the price of the item.
-->
<!ELEMENT DeductedPrice (Money)>

<!--
    Defines a Purchasing Card element used for payment.
-->
<!ELEMENT PCard (PostalAddress?)>
<!--
	@attr number Number of the Purchasing Card.
	
	@attr expiration Expiration date of the Purchasing Card.
	
	@attr name Name of the Purchasing Card.
-->
<!ATTLIST PCard
    number      %number;  #REQUIRED
    expiration  %date;    #REQUIRED
    name        %string;  #IMPLIED
>

<!--
    Defines a Virtual Payment Token Card element used for payment.
-->
<!ELEMENT PaymentToken EMPTY>

<!--
	@attr number Number of the Virtual Card.
	@attr expiration Expiration date of the Virtual Card.
	@attr name Name of the Virtual Card.
	@attr token Token of the Virtual Card.
-->
<!ATTLIST PaymentToken
	number      %number;  #REQUIRED
	expiration  %date;    #REQUIRED
	name        %string;  #IMPLIED
	token	%string; #REQUIRED
>

<!--
    Represents the total sum of all the charges  applied on the goods and 
    services. This can appear at the line item and summary in an invoice.
-->
<!ELEMENT TotalCharges (Money)>

<!--
    Represents the total sum of all the allowances applied on the goods and 
    services. This can appear at the line item and summary in an invoice.
-->
<!ELEMENT TotalAllowances (Money)>

<!--
    Represents the sum of the SubTotal, Charges (including special handling 
    charges and shipping charges), and Allowances (including discounts). This 
	does not include taxes. This can appear at the line item and summary in 
	an invoice.
-->
<!ELEMENT TotalAmountWithoutTax (Money)>

