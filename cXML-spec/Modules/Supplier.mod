<!--
    For cXML license agreement information, please see
    http://www.cxml.org/home/license.asp

    $Id$
-->

<!--
    Supplier of goods and services. Includes a list of SupplierIDs which
    identify the Supplier.
-->
<!ELEMENT Supplier (Name, Comments?, SupplierID+, SupplierLocation*)>
<!-- 
	@attr corporateURL URL to web site about the supplier.

    @attr storeFrontURL URL to web site where a user can shop or browse. 
-->
<!ATTLIST Supplier
    corporateURL   %URL;  #IMPLIED
    storeFrontURL  %URL;  #IMPLIED
>

<!--
    One of the locations for a supplier. Supplier location is
    generally a physical location.
-->
<!ELEMENT SupplierLocation (Address, OrderMethods)>

<!--
    OrderMethods is the list of methods by which one can order
    from a supplier. The contact element is the technical contact
    who should be able to assist with order processing issues.
    The list is to be ordered by supplier preference, the first
    element having the highest degree of preference.
-->
<!ELEMENT OrderMethods (OrderMethod+, Contact?)>

<!--
    OrderMethod is a method for ordering. It is comprised of a
    target address for the order and the protocol expected by
    the address.
-->
<!ELEMENT OrderMethod (OrderTarget, OrderProtocol?)>

<!--
    OrderTarget represents an address to which orders can be
    sent.
-->
<!ELEMENT OrderTarget (Phone | Email | Fax | URL | OtherOrderTarget)>

<!--
    OrderProtocol is the communication method to be used when
    communicating an order to a supplier. An example would be "cXML".
-->
<!ELEMENT OrderProtocol (#PCDATA)> <!-- string -->

<!--
    OtherOrderTarget represents an address which is not enumerated by
    default in the OrderTarget Element. This may contain address targets
    beyond the ability of this document to describe.

-->
<!ELEMENT OtherOrderTarget ANY>
<!-- 
	@attr name Optional name for target. 
-->
<!ATTLIST OtherOrderTarget
    name  %string;  #IMPLIED
>

<!--
    Definition of a supplier ID. A supplier ID is a (domain, value)
    pair so that suppliers have the flexibility to define their ID's
    according to an arbitrary convention (for example, (DUNS, 12345),
    (TaxID, 88888888)).
-->
<!ELEMENT SupplierID (#PCDATA)> <!-- string -->
<!-- 
	@attr domain The domain of the ID. 
-->
<!ATTLIST SupplierID
    domain  %string;  #REQUIRED
>
<!--
   Defines a List of Suppliers that might be associated with a quote Item. Used in
   ItemOut. 
-->
<!ELEMENT SupplierList (Supplier+)>
