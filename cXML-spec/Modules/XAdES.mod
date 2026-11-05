<!--
    For cXML license agreement information, please see
    http://www.cxml.org/home/license.asp

    $Id: //ariba/cxml/Modules/Profile.mod#6 $
-->

<!--
    For more information about XAdES, refer to the following URL.
    Portions of this DTD are derived from information contained therein.
    http://uri.etsi.org/01903/v1.3.2#
-->

<!ENTITY % Any.ANY ''>
<!ENTITY % XMLTimeStamp.ANY ''>

<!-- Start Any -->

<!ELEMENT xades:Any (#PCDATA   %Any.ANY;)*>

<!-- End Any -->

<!-- Start ObjectIdentifier -->

<!ELEMENT xades:ObjectIdentifier (xades:Identifier, xades:Description?, 
xades:DocumentationReferences?)>
<!ELEMENT xades:Identifier (#PCDATA)>
<!ATTLIST xades:Identifier
	Qualifier (OIDAsURI | OIDAsURN) #IMPLIED
>
<!ELEMENT xades:Description (#PCDATA)>
<!ELEMENT xades:DocumentationReferences (xades:DocumentationReference)+>
<!ELEMENT xades:DocumentationReference (#PCDATA)>

<!-- End ObjectIdentifier -->

<!-- Start EncapsulatedPKIData -->

<!ELEMENT xades:EncapsulatedPKIData (#PCDATA)>
<!ATTLIST xades:EncapsulatedPKIData
	Id ID #IMPLIED
	Encoding CDATA #IMPLIED
>

<!-- End EncapsulatedPKIData -->

<!-- Start time-stamp container types -->

<!ELEMENT xades:Include EMPTY>
<!ATTLIST xades:Include
	URI CDATA #REQUIRED
	referencedData CDATA #IMPLIED
>

<!ELEMENT xades:ReferenceInfo (ds:DigestMethod, ds:DigestValue)>
<!ATTLIST xades:ReferenceInfo
	Id ID #IMPLIED
	URI CDATA #IMPLIED
>
<!ELEMENT xades:XAdESTimeStamp (xades:Include*, ds:CanonicalizationMethod?, 
(xades:EncapsulatedTimeStamp | xades:XMLTimeStamp)+)>
<!ATTLIST xades:XAdESTimeStamp
	Id ID #IMPLIED
>

<!ELEMENT xades:OtherTimeStamp (xades:ReferenceInfo+, ds:CanonicalizationMethod?, 
(xades:EncapsulatedTimeStamp | xades:XMLTimeStamp))>
<!ATTLIST xades:OtherTimeStamp
	Id ID #IMPLIED
>

<!ELEMENT xades:EncapsulatedTimeStamp (#PCDATA)>
<!ATTLIST xades:EncapsulatedTimeStamp
	Id ID #IMPLIED
>

<!ELEMENT xades:XMLTimeStamp (#PCDATA  %XMLTimeStamp.ANY; )*>

<!-- End time-stamp tokens container  -->

<!-- Start container types -->

<!-- Start QualifyingProperties -->

<!ELEMENT xades:QualifyingProperties (xades:SignedProperties?, xades:UnsignedProperties?)>
<!ATTLIST xades:QualifyingProperties
	Target CDATA #REQUIRED
	Id ID #IMPLIED
	xmlns:xades	CDATA	#FIXED 'http://uri.etsi.org/01903/v1.3.2#'
>

<!ELEMENT xades:SignedProperties (xades:SignedSignatureProperties, 
xades:SignedDataObjectProperties?)>
<!ATTLIST xades:SignedProperties
	Id ID #IMPLIED
>

<!ELEMENT xades:UnsignedProperties (xades:UnsignedSignatureProperties?, 
xades:UnsignedDataObjectProperties?)>
<!ATTLIST xades:UnsignedProperties
	Id ID #IMPLIED
>

<!-- End QualifyingProperties -->

<!-- Start SignedSignatureProperties, SignedDataObjectProperties, 
UnsignedSignatureProperties, UnsignedDataObjectProperties -->

<!ELEMENT xades:SignedSignatureProperties (xades:SigningTime?, xades:SigningCertificate?, 
xades:SignaturePolicyIdentifier?, xades:SignatureProductionPlace?, xades:SignerRole?)>
<!ATTLIST xades:SignedSignatureProperties
	Id ID #IMPLIED
>

<!ELEMENT xades:SignedDataObjectProperties (xades:DataObjectFormat*, 
xades:CommitmentTypeIndication*, xades:AllDataObjectsTimeStamp*, 
xades:IndividualDataObjectsTimeStamp*)>
<!ATTLIST xades:SignedDataObjectProperties
	Id ID #IMPLIED
>

<!ELEMENT xades:UnsignedSignatureProperties (xades:CounterSignature | xades:SignatureTimeStamp 
| xades:CompleteCertificateRefs | xades:CompleteRevocationRefs | xades:AttributeCertificateRefs
 | xades:AttributeRevocationRefs | (xades:SigAndRefsTimeStamp | xades:RefsOnlyTimeStamp) | 
xades:CertificateValues | xades:RevocationValues | xades:AttrAuthoritiesCertValues 
| xades:AttributeRevocationValues | xades:ArchiveTimeStamp)+>
<!ATTLIST xades:UnsignedSignatureProperties
	Id ID #IMPLIED
>

<!ELEMENT xades:UnsignedDataObjectProperties (xades:UnsignedDataObjectProperty*)>
<!ATTLIST xades:UnsignedDataObjectProperties
	Id ID #IMPLIED
>

<!ELEMENT xades:UnsignedDataObjectProperty (#PCDATA   %Any.ANY;)*>

<!-- End SignedSignatureProperties, SignedDataObjectProperties, 
UnsignedSignatureProperties, UnsignedDataObjectProperties -->

<!-- Start QualifyingPropertiesReference -->

<!ELEMENT xades:QualifyingPropertiesReference EMPTY>
<!ATTLIST xades:QualifyingPropertiesReference
	URI CDATA #REQUIRED
	Id ID #IMPLIED
>

<!-- End QualifyingPropertiesReference -->

<!-- End container types -->

<!-- Start SigningTime -->

<!ELEMENT xades:SigningTime (#PCDATA)>

<!-- End SigningTime -->

<!-- Start SigningCertificate -->

<!ELEMENT xades:SigningCertificate (xades:Cert+)>
<!ELEMENT xades:Cert (xades:CertDigest, xades:IssuerSerial)>
<!ATTLIST xades:Cert
	URI CDATA #IMPLIED
>
<!ELEMENT xades:CertDigest (ds:DigestMethod, ds:DigestValue)>
<!ELEMENT xades:IssuerSerial (ds:X509IssuerName, ds:X509SerialNumber)>
<!ELEMENT xades:X509IssuerName (#PCDATA)>
<!ELEMENT xades:X509SerialNumber (#PCDATA)>

<!-- End SigningCertificate -->

<!-- Start SignaturePolicyIdentifier -->

<!ELEMENT xades:SignaturePolicyIdentifier (xades:SignaturePolicyId | 
xades:SignaturePolicyImplied)>
<!ELEMENT xades:SignaturePolicyImplied ANY>
<!ELEMENT xades:SignaturePolicyId (xades:SigPolicyId, ds:Transforms?, xades:SigPolicyHash, 
xades:SigPolicyQualifiers?)>
<!ELEMENT xades:SigPolicyId (xades:Identifier, xades:Description?, xades:DocumentationReferences?)>
<!ELEMENT xades:SigPolicyHash (ds:DigestMethod, ds:DigestValue)>
<!ELEMENT xades:SigPolicyQualifiers (xades:SigPolicyQualifier+)>
<!ELEMENT xades:SigPolicyQualifier (#PCDATA|xades:SPURI %Any.ANY; )*>

<!-- End SignaturePolicyIdentifier -->

<!-- Start SPURI and SPUserNotice -->

<!ELEMENT xades:SPURI (#PCDATA)>
<!ELEMENT xades:SPUserNotice (xades:NoticeRef?, xades:ExplicitText?)>
<!ELEMENT xades:NoticeRef (xades:Organization, xades:NoticeNumbers)>
<!ELEMENT xades:ExplicitText (#PCDATA)>
<!ELEMENT xades:Organization (#PCDATA)>
<!ELEMENT xades:NoticeNumbers (xades:int*)>
<!ELEMENT xades:int (#PCDATA)>

<!-- End SPURI and SPUserNotice -->

<!-- Start CounterSignature -->

<!ELEMENT xades:CounterSignature (ds:Signature)>

<!-- End CounterSignature -->

<!-- Start DataObjectFormat -->

<!ELEMENT xades:DataObjectFormat (xades:Description?, xades:ObjectIdentifier?, xades:MimeType?, 
xades:Encoding?)>
<!ATTLIST xades:DataObjectFormat
	ObjectReference CDATA #REQUIRED
>
<!ELEMENT xades:MimeType (#PCDATA)>
<!ELEMENT xades:Encoding (#PCDATA)>

<!-- End DataObjectFormat -->

<!-- Start CommitmentTypeIndication -->

<!ELEMENT xades:CommitmentTypeIndication (xades:CommitmentTypeId, (xades:ObjectReference+ | 
xades:AllSignedDataObjects), xades:CommitmentTypeQualifiers?)>
<!ELEMENT xades:CommitmentTypeId (xades:Identifier, xades:Description?, 
xades:DocumentationReferences?)>
<!ELEMENT xades:ObjectReference (#PCDATA)>
<!ELEMENT xades:AllSignedDataObjects ANY>
<!ELEMENT xades:CommitmentTypeQualifiers (xades:CommitmentTypeQualifier*)>
<!ELEMENT xades:CommitmentTypeQualifier (#PCDATA %Any.ANY; )*>

<!-- End CommitmentTypeIndication -->

<!-- Start SignatureProductionPlace -->

<!ELEMENT xades:SignatureProductionPlace (xades:City?, xades:StateOrProvince?, xades:PostalCode?, 
xades:CountryName?)>
<!ELEMENT xades:City (#PCDATA)>
<!ELEMENT xades:StateOrProvince (#PCDATA)>
<!ELEMENT xades:PostalCode (#PCDATA)>
<!ELEMENT xades:CountryName (#PCDATA)>

<!-- End SignatureProductionPlace -->

<!-- Start SignerRole -->

<!ELEMENT xades:SignerRole (xades:ClaimedRoles?, xades:CertifiedRoles?)>
<!ELEMENT xades:ClaimedRoles (xades:ClaimedRole+)>
<!ELEMENT xades:CertifiedRoles (xades:CertifiedRole+)>
<!ELEMENT xades:ClaimedRole (#PCDATA %Any.ANY; )*>
<!ELEMENT xades:CertifiedRole (#PCDATA)>
<!ATTLIST xades:CertifiedRole
	Id ID #IMPLIED
	Encoding CDATA #IMPLIED
>

<!-- End SignerRole -->

<!-- Start AllDataObjectsTimeStamp, IndividualDataObjectsTimeStamp, 
SignatureTimeStamp -->

<!ELEMENT xades:AllDataObjectsTimeStamp (ds:CanonicalizationMethod?, 
(xades:EncapsulatedTimeStamp | xades:XMLTimeStamp)+)>
<!ATTLIST xades:AllDataObjectsTimeStamp
	Id ID #IMPLIED
>

<!ELEMENT xades:IndividualDataObjectsTimeStamp (xades:Include+, ds:CanonicalizationMethod?, 
(xades:EncapsulatedTimeStamp | xades:XMLTimeStamp)+)>
<!ATTLIST xades:IndividualDataObjectsTimeStamp
	Id ID #IMPLIED
>

<!ELEMENT xades:SignatureTimeStamp (ds:CanonicalizationMethod?, (xades:EncapsulatedTimeStamp 
| xades:XMLTimeStamp)+)>
<!ATTLIST xades:SignatureTimeStamp
	Id ID #IMPLIED
>

<!-- End AllDataObjectsTimeStamp, IndividualDataObjectsTimeStamp, 
SignatureTimeStamp -->

<!-- Start CompleteCertificateRefs -->

<!ELEMENT xades:CompleteCertificateRefs (xades:CertRefs)>
<!ATTLIST xades:CompleteCertificateRefs
	Id ID #IMPLIED
>
<!ELEMENT xades:CertRefs (xades:Cert+)>

<!-- End CompleteCertificateRefs -->

<!-- Start AttributeCertificateRefs -->

<!ELEMENT xades:AttributeCertificateRefs (xades:CertRefs)>
<!ATTLIST xades:AttributeCertificateRefs
	Id ID #IMPLIED
>

<!-- End AttributeCertificateRefs -->

<!-- Start CompleteRevocationRefs -->

<!ELEMENT xades:CompleteRevocationRefs (xades:CRLRefs?, xades:OCSPRefs?, xades:OtherRefs?)>
<!ATTLIST xades:CompleteRevocationRefs
	Id ID #IMPLIED
>
<!ELEMENT xades:CRLRefs (xades:CRLRef+)>
<!ELEMENT xades:OCSPRefs (xades:OCSPRef+)>
<!ELEMENT xades:OtherRefs (xades:OtherRef+)>

<!ELEMENT xades:CRLRef (xades:DigestAlgAndValue, xades:CRLIdentifier?)>
<!ELEMENT xades:OCSPRef (xades:OCSPIdentifier, xades:DigestAlgAndValue?)>
<!ELEMENT xades:OtherRef (#PCDATA %Any.ANY; )*>

<!ELEMENT xades:DigestAlgAndValue (ds:DigestMethod, ds:DigestValue)>
<!ELEMENT xades:CRLIdentifier (xades:Issuer, xades:IssueTime, xades:Number?)>
<!ATTLIST xades:CRLIdentifier
	URI CDATA #IMPLIED
>
<!ELEMENT xades:OCSPIdentifier (xades:ResponderID, xades:ProducedAt)>
<!ATTLIST xades:OCSPIdentifier
	URI CDATA #IMPLIED
>

<!ELEMENT xades:Issuer (#PCDATA)>
<!ELEMENT xades:IssueTime (#PCDATA)>
<!ELEMENT xades:Number (#PCDATA)>

<!ELEMENT xades:ResponderID (xades:ByName | xades:ByKey)>
<!ELEMENT xades:ByName  (#PCDATA)>
<!ELEMENT xades:ByKey  (#PCDATA)>
<!ELEMENT xades:ProducedAt (#PCDATA)>

<!-- End CompleteRevocationRefs -->

<!-- Start AttributeRevocationRefs -->

<!ELEMENT xades:AttributeRevocationRefs (xades:CRLRefs?, xades:OCSPRefs?, xades:OtherRefs?)>
<!ATTLIST xades:AttributeRevocationRefs
	Id ID #IMPLIED
>

<!-- End AttributeRevocationRefs -->

<!-- Start SigAndRefsTimeStamp, RefsOnlyTimeStamp  -->

<!ELEMENT xades:SigAndRefsTimeStamp (xades:Include*, ds:CanonicalizationMethod?, 
(xades:EncapsulatedTimeStamp | xades:XMLTimeStamp)+)>
<!ATTLIST xades:SigAndRefsTimeStamp
	Id ID #IMPLIED
>

<!ELEMENT xades:RefsOnlyTimeStamp (xades:Include*, ds:CanonicalizationMethod?, 
(xades:EncapsulatedTimeStamp | xades:XMLTimeStamp)+)>
<!ATTLIST xades:RefsOnlyTimeStamp
	Id ID #IMPLIED
>

<!-- End SigAndRefsTimeStamp, RefsOnlyTimeStamp  -->

<!-- Start CertificateValues -->

<!ELEMENT xades:CertificateValues (xades:EncapsulatedX509Certificate | 
xades:OtherCertificate)*>
<!ATTLIST xades:CertificateValues
	Id ID #IMPLIED
>

<!ELEMENT xades:EncapsulatedX509Certificate (#PCDATA)>
<!ATTLIST xades:EncapsulatedX509Certificate
	Id ID #IMPLIED
	Encoding CDATA #IMPLIED
>
<!ELEMENT xades:OtherCertificate (#PCDATA %Any.ANY;)*>

<!ELEMENT xades:AttrAuthoritiesCertValues (xades:EncapsulatedX509Certificate | 
xades:OtherCertificate)*>
<!ATTLIST xades:AttrAuthoritiesCertValues
	Id ID #IMPLIED
>

<!-- Start RevocationValues -->

<!ELEMENT xades:RevocationValues (xades:CRLValues?, xades:OCSPValues?, xades:OtherValues?)>
<!ATTLIST xades:RevocationValues
	Id ID #IMPLIED
>

<!ELEMENT xades:CRLValues (xades:EncapsulatedCRLValue+)>
<!ELEMENT xades:OCSPValues (xades:EncapsulatedOCSPValue+)>
<!ELEMENT xades:OtherValues (xades:OtherValue+)>

<!ELEMENT xades:EncapsulatedCRLValue (#PCDATA)>
<!ATTLIST xades:EncapsulatedCRLValue
	Id ID #IMPLIED
	Encoding CDATA #IMPLIED
>
<!ELEMENT xades:EncapsulatedOCSPValue (#PCDATA)>
<!ATTLIST xades:EncapsulatedOCSPValue
	Id ID #IMPLIED
	Encoding CDATA #IMPLIED
>
<!ELEMENT xades:OtherValue (#PCDATA %Any.ANY;  )*>

<!-- End RevocationValues -->

<!ELEMENT xades:AttributeRevocationValues (xades:CRLValues?, xades:OCSPValues?, xades:OtherValues?)>
<!ATTLIST xades:AttributeRevocationValues
	Id ID #IMPLIED
>
<!-- Start ArchiveTimeStamp -->

<!ELEMENT xades:ArchiveTimeStamp (xades:Include*, ds:CanonicalizationMethod?, 
(xades:EncapsulatedTimeStamp | xades:XMLTimeStamp)+)>
<!ATTLIST xades:ArchiveTimeStamp
	Id ID #IMPLIED
>


<!-- End ArchiveTimeStamp -->
