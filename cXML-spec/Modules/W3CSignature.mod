<!--
    For cXML license agreement information, please see
    http://www.cxml.org/home/license.asp

    $Id: //ariba/cxml/Modules/Profile.mod#6 $
-->

<!--
    For more information about W3C XML signatures, refer to the
    following URL. Portions of this DTD are derived from information
    contained therein.
    http://www.w3.org/TR/xmldsig-core/
-->

<!-- DTD for XML Signatures
    http://www.w3.org/2000/09/xmldsig#
    Joseph Reagle $last changed 20001215$

    http://www.w3.org/2000/09/xmldsig#
    $Revision: 1.1 $ on $Date: 2002/02/08 20:32:26 $ by $Author: reagle $

    Copyright 2001 The Internet Society and W3C (Massachusetts Institute
    of Technology, Institut National de Recherche en Informatique et en
    Automatique, Keio University). All Rights Reserved.
    http://www.w3.org/Consortium/Legal/

    This document is governed by the W3C Software License [1] as described
    in the FAQ [2].

    [1] http://www.w3.org/Consortium/Legal/copyright-software-19980720
    [2] http://www.w3.org/Consortium/Legal/IPR-FAQ-20000620.html#DTD
-->

<!--

The following entity declarations enable external/flexible content in
the Signature content model.

#PCDATA emulates schema string; when combined with element types it
emulates schema's mixed content type.

%foo.ANY permits the user to include their own element types from
other namespaces, for example:
 <!ENTITY % KeyValue.ANY '| ecds:ECDSAKeyValue'>
 ...
 <!ELEMENT ecds:ECDSAKeyValue (#PCDATA)  >

-->

<!ENTITY % Object.ANY '|xades:QualifyingProperties|cXMLSignedInfo|Extrinsic'>
<!ENTITY % Method.ANY ''>
<!ENTITY % Transform.ANY ''>
<!ENTITY % SignatureProperty.ANY ''>
<!ENTITY % KeyInfo.ANY ''>
<!ENTITY % KeyValue.ANY ''>
<!ENTITY % PGPData.ANY ''>
<!ENTITY % X509Data.ANY ''>
<!ENTITY % SPKIData.ANY ''>



<!-- Start Core Signature declarations, these should NOT be altered -->

<!ELEMENT ds:Signature (ds:SignedInfo, ds:SignatureValue, ds:KeyInfo?, ds:Object*)  >
<!ATTLIST ds:Signature  
	xmlns:ds	CDATA	#FIXED 'http://www.w3.org/2000/09/xmldsig#'
	xmlns:xades	CDATA	#FIXED 'http://uri.etsi.org/01903/v1.3.2#'
	Id  		ID	#IMPLIED >

<!ELEMENT ds:SignatureValue (#PCDATA) >
<!ATTLIST ds:SignatureValue  
         Id  ID      #IMPLIED>

<!ELEMENT ds:SignedInfo (ds:CanonicalizationMethod, 
	ds:SignatureMethod,  ds:Reference+)  >
<!ATTLIST ds:SignedInfo  
	Id	 ID 	 #IMPLIED 
>

<!ELEMENT ds:CanonicalizationMethod (#PCDATA %Method.ANY;)* > 
<!ATTLIST ds:CanonicalizationMethod 
	Algorithm CDATA #REQUIRED > 

<!ELEMENT ds:SignatureMethod (#PCDATA|ds:HMACOutputLength %Method.ANY;)* >
<!ATTLIST ds:SignatureMethod 
	Algorithm CDATA #REQUIRED > 

<!ELEMENT ds:Reference (ds:Transforms?, ds:DigestMethod, ds:DigestValue)  >
<!ATTLIST ds:Reference
	Id	ID	#IMPLIED
	URI	CDATA	#IMPLIED
	Type	CDATA	#IMPLIED>


<!ELEMENT ds:Transforms (ds:Transform+)>

<!ELEMENT ds:Transform (#PCDATA|ds:XPath %Transform.ANY;)* >
<!ATTLIST ds:Transform 
	Algorithm    CDATA    #REQUIRED >

<!ELEMENT ds:XPath (#PCDATA) >

<!ELEMENT ds:DigestMethod (#PCDATA %Method.ANY;)* >
<!ATTLIST ds:DigestMethod  
	Algorithm		CDATA	#REQUIRED >

<!ELEMENT ds:DigestValue  (#PCDATA)  >

<!ELEMENT ds:KeyInfo	(#PCDATA|ds:KeyName|ds:KeyValue|ds:RetrievalMethod|
           ds:X509Data|ds:PGPData|ds:SPKIData|ds:MgmtData %KeyInfo.ANY;)* >
<!ATTLIST ds:KeyInfo
	Id	ID	 #IMPLIED >

<!-- Key Information -->

<!ELEMENT ds:KeyName (#PCDATA) >
<!ELEMENT ds:KeyValue (#PCDATA|ds:DSAKeyValue|ds:RSAKeyValue %KeyValue.ANY;)* >
<!ELEMENT ds:MgmtData (#PCDATA) >

<!ELEMENT ds:RetrievalMethod (ds:Transforms?) >
<!ATTLIST ds:RetrievalMethod
  URI	CDATA #REQUIRED 
  Type	CDATA #IMPLIED > 

<!-- X.509 Data -->

<!ELEMENT ds:X509Data ((ds:X509IssuerSerial | ds:X509SKI | ds:X509SubjectName |
                    ds:X509Certificate | ds:X509CRL )+ %X509Data.ANY;)>
<!ELEMENT ds:X509IssuerSerial (ds:X509IssuerName, ds:X509SerialNumber) >
<!ELEMENT ds:X509IssuerName (#PCDATA) >
<!ELEMENT ds:X509SubjectName (#PCDATA) >
<!ELEMENT ds:X509SerialNumber (#PCDATA) >
<!ELEMENT ds:X509SKI (#PCDATA) >
<!ELEMENT ds:X509Certificate (#PCDATA) >
<!ELEMENT ds:X509CRL (#PCDATA) >

<!-- PGPData -->

<!ELEMENT ds:PGPData ((ds:PGPKeyID, ds:PGPKeyPacket?) | (ds:PGPKeyPacket) %PGPData.ANY;) >
<!ELEMENT ds:PGPKeyPacket  (#PCDATA)  >
<!ELEMENT ds:PGPKeyID  (#PCDATA)  >

<!-- SPKI Data -->

<!ELEMENT ds:SPKIData (ds:SPKISexp %SPKIData.ANY;)  >
<!ELEMENT ds:SPKISexp  (#PCDATA)  >

<!-- Extensible Content -->

<!ELEMENT ds:Object (#PCDATA|ds:Signature|ds:SignatureProperties|ds:Manifest %Object.ANY;)* >
<!ATTLIST ds:Object  
	Id	ID	#IMPLIED
	MimeType	CDATA	#IMPLIED
	Encoding	CDATA	#IMPLIED >

<!ELEMENT ds:Manifest (ds:Reference+)  >
<!ATTLIST ds:Manifest  
	Id	ID	#IMPLIED >

<!ELEMENT ds:SignatureProperties (ds:SignatureProperty+)  >
<!ATTLIST ds:SignatureProperties  
	Id	ID	 #IMPLIED  >

<!ELEMENT ds:SignatureProperty (#PCDATA %SignatureProperty.ANY;)* >
<!ATTLIST ds:SignatureProperty  
	Target 	CDATA	 #REQUIRED
	Id	ID	 #IMPLIED  >

<!-- Algorithm Parameters -->

<!ELEMENT ds:HMACOutputLength (#PCDATA) >

<!ELEMENT ds:DSAKeyValue ((ds:P, ds:Q)?, ds:G?, ds:Y, ds:J?, (ds:Seed, ds:PgenCounter)?) >
<!ELEMENT ds:P (#PCDATA) >
<!ELEMENT ds:Q (#PCDATA) >
<!ELEMENT ds:G (#PCDATA) >
<!ELEMENT ds:Y (#PCDATA) >
<!ELEMENT ds:J (#PCDATA) >
<!ELEMENT ds:Seed (#PCDATA) >
<!ELEMENT ds:PgenCounter (#PCDATA) >

<!ELEMENT ds:RSAKeyValue (ds:Modulus, ds:Exponent) > 
<!ELEMENT ds:Modulus (#PCDATA) >
<!ELEMENT ds:Exponent (#PCDATA) >
