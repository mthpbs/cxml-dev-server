<!--
    For cXML license agreement information, please see
    http://www.cxml.org/home/license.asp

    $Id: $
-->

<!-- ====
    Allow this module to be built separately from any other DTD file.
==== -->

<!--
    Describes a time card document that is sent from a supplier/staffing agency to a buyer.
-->
<!ELEMENT TimeCardRequest (TimeCard)>

<!--
    Describes a time card document that is sent from a buyer to a supplier/staffing agency.
-->
<!ELEMENT TimeCardInfoRequest (TimeCard)>

<!--
    Definition of a Time card that is used to capture time and expense 
	entered by a contractor/temp labor. Document reference is for update 
	to timecards. Approval info is only when suppliers send in timecards 
	that are approved in their system. There can be multiple approval 
	infos since many people could have approved it. The OrderInfo element 
	is used to reference an order. One timecard can reference only one 
	order. Also the document reference is used on an update operation to 
	refer to a previous TimeCardRequest/TimeCardInfoRequest. 
-->
<!ELEMENT TimeCard (OrderInfo, Contractor, ReportedTime, SubmitterInfo, ApprovalInfo*, Comments?, DocumentReference?)>
<!--
	@attr type The type of the timecard (enumeration). Defaults to "new". To update the 
		status of the timecard, one needs to do an update to the original timecard 
		with the new status in it.

    @attr status The status of the timecard (enumeration). Defaults to "submitted".

	@attr timeCardID The buyer system/supplier system unique identifier for this 
		timecard.
-->
<!ATTLIST TimeCard
    type      	(new | update | delete)  "new"
  	status		(submitted| approved| denied)  "submitted"
    timeCardID      NMTOKEN #REQUIRED
>

<!--
	Definition of ReportedTime. This captures the line items for
	the time card. Though the DTD allows it, the expectation is
	that at least one of TimeCardTimeInterval or Expense will be
	present.
-->
<!ELEMENT ReportedTime (Period, TimeCardTimeInterval*, Expense*)>

<!--
    Definition of a time interval that is being reported on a timecard. 
	Extrinsic are used to pass other things not captured. For the 
	TimeRange the start date is mandatory. The difference in time between 
	the endDate and startDate attributes in TimeRange need not agree with 
	the duration. Hence the endDate attribute is for informational purpose 
	only. 
-->
<!ELEMENT TimeCardTimeInterval (TimeRange, Comments?, Extrinsic*)>
<!--
	@attr duration The duration of time being claimed for the line item, represented 
		in the ISO 8601 format PnYn MnDTnH nMnS, where nY represents the number of years, nM the 
		number of months, nD the number of days, T the date/time separator, nH the number of hours, 
		nM the number of minutes and nS the number of seconds. For example, to indicate a duration 
		of 1 year, 2 months, 3 days, 10 hours, and 30 minutes, one would write: P1Y2M3DT10H30M.
		
		In the event that duration and TimeRange do not agree, duration takes precedence. For example, 
		if duration is 2 hours, and TimeRange is from 4:00 p.m. to 8:00 p.m., then the 2 hour duration 
		takes precedence. However, if duration is not present, then it is computed from the TimeRange.
    
	@attr paycode The pay code to be used. Recommended pay codes include:
		<ul>
		<li>Regular</li>
		<li>Overtime</li>
		<li>Doubletime</li>
		<li>Mealbreak</li>
		<li>Tripletime</li>
		<li>WeeklyRestDay</li>
		<li>HolidayWorked</li>
		<li>RegularNightShift</li>
		<li>OvertimeNightShift</li>
		<li>DoubletimeNightShift</li>
		<li>TripletNightShift</li>
		<li>WeeklyRestDayNightShift</li>
		<li>RegularMixedShift</li>
		<li>OvertimeMixedShift</li>
		<li>DoubletimeMixedShift</li>
		<li>TripletimeMixedShift</li>
		<li>WeeklyRestDayMixedShift</li>
		</ul>

    @attr isNonBillable Set to "yes" to indicate that the time is not billable. 
		The default behavior is billable. 
-->
<!ATTLIST TimeCardTimeInterval
        duration     	%duration; #REQUIRED 
        payCode     	%string; #REQUIRED 
        isNonBillable   (yes)  #IMPLIED
>

<!--
	Definition of an expense item. The type of expense, date and
    amount are required pieces of information
-->
<!ELEMENT Expense (ExpenseAmount, Comments*, Extrinsic*)>
<!--
	@attr expenseDate Date of the expense.
	
    @attr expenseType The type of expense. Recommended expense types include:
		<ul>
		<li>mileage</li>
		<li>airfare</li>
		<li>fuel</li>
		<li>taxi</li>
		<li>perDiem</li>
		<li>hotel</li>
		</ul>
		
    @attr isNonBillable Set to "yes" to indicate that the time is not billable. 
		The default behavior is billable.
-->
<!ATTLIST Expense
        expenseDate	%date;  #REQUIRED
        expenseType    	%string; #REQUIRED 
        isNonBillable   (yes)  #IMPLIED
>

<!--
	Amount of the expense item.
-->
<!ELEMENT ExpenseAmount (Money)>

<!--
   	Information about someone submitting a timecard. If the contact 
	is absent, then it is assumed that the contractor is the submitter 
	of the timecard.
-->
<!ELEMENT SubmitterInfo (Contact?)>
<!--
	@attr submittedDate The time when the timecard was submitted.
-->
<!ATTLIST SubmitterInfo
        submittedDate     	%datetime.tz; #REQUIRED
>

<!--
    Information about the approver for a timecard. This is sent by the 
	supplier for informational purposes only. The buyer system does 
	not send it. This can includes all the approvers in the chain.
-->
<!ELEMENT ApprovalInfo (Contact?)>
<!--
	@attr approvedDate Time when the timecard was approved.
-->
<!ATTLIST ApprovalInfo
        approvedDate     	%datetime.tz; #REQUIRED
>

