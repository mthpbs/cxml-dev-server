<!--
    For cXML license agreement information, please see
    http://www.cxml.org/home/license.asp

    $Id$
-->

<!--
    Captures spend detail information. SpendDetail can
    be used in the ItemIn and ItemOut and for the following types of
    messages:

        <ul>
		<li>PunchOutSetupRequest</li>
        <li>PunchOutOrderMessage</li>
        <li>OrderRequest</li>
        <li>ConfirmationRequest</li>
		</ul>
		<p>But not for:</p>
		<ul>
		<li>MasterAgreementRequest</li>
		</ul>

    TravelDetail: Travel details for the current travel line item.

    LaborDetail: Labor details for the current temporary labor line item.

    FeeDetail: Fee details for the current fee line item.
        
    Extrinsic: Detail information for the any undefined spend category. The name
    	attribute of the Extrinsic element should specify the type of the
    	spend category (for example, (PrintDetail, ProjectLaborDetail)).
-->
<!ELEMENT SpendDetail (TravelDetail | FeeDetail | LaborDetail | Extrinsic)>

<!--
    FeeDetail contains additional information about a line item for a fee, 
    which may or may not be recurring.

    UnitRate: The amount(s) to be paid per unit (of time or other measure).
        In the case of multiple UnitRates (that is, a "rate schedule"), it is
        customary and expected that each UnitRate will include a
        TermReference to distinguish them.

    Period: The Period of time over which the fee is charged, if applicable.
-->
<!ELEMENT FeeDetail (UnitRate+, Period?) >
<!--
    @attr isRecurring Indicates that the fee is recurring (charged more than once).

-->
<!ATTLIST FeeDetail
	isRecurring (yes) #IMPLIED
>
