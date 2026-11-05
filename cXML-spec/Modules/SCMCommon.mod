<!-- 
	Inventory that is in the possession of the customer, and is owned and managed by the customer.
-->
<!ELEMENT Inventory (SubcontractingStockInTransferQuantity?, UnrestrictedUseQuantity?, 
BlockedQuantity?, QualityInspectionQuantity?, PromotionQuantity?,
StockInTransferQuantity?, IncrementQuantity?, RequiredMinimumQuantity?,
RequiredMaximumQuantity?,
StockOnHandQuantity?, WorkInProcessQuantity?, IntransitQuantity?, ScrapQuantity?,
OrderQuantity?, DaysOfSupply?)>

<!-- 
	Consignment Inventory is inventory that is in the possession of the customer, but is still owned by the supplier.
-->
<!ELEMENT ConsignmentInventory (SubcontractingStockInTransferQuantity?,
UnrestrictedUseQuantity?, BlockedQuantity?, 
QualityInspectionQuantity?, PromotionQuantity?, StockInTransferQuantity?, 
IncrementQuantity?, RequiredMinimumQuantity?, RequiredMaximumQuantity?)>

<!--
	SubcontractingStockInTransfer is the transfer of stock to a vendor of type subcontracting using a special movement type. 
-->
<!ELEMENT SubcontractingStockInTransferQuantity (UnitOfMeasure?)  >
<!--
    @attr quantity Number of stock items in transfer.
-->
<!ATTLIST SubcontractingStockInTransferQuantity
    quantity        %r8;       #IMPLIED>

<!-- 
	Unrestricted Stock is the physical stock that is always available at a plant/storage location that can be 
	consumed for stock movements and available for Material requirements planning.
-->
<!ELEMENT UnrestrictedUseQuantity (UnitOfMeasure?)  >
<!--
    @attr quantity Number of unrestricted stock items.
-->
<!ATTLIST UnrestrictedUseQuantity
    quantity        %r8;       #IMPLIED>

<!--
	Blocked stock is not counted as unrestricted stock .You can have Plant level configuration to consider 
	the stock for MRP.
-->
<!ELEMENT BlockedQuantity (UnitOfMeasure?)  >
<!--
    @attr quantity Number of blocked stock items.
-->
<!ATTLIST BlockedQuantity
    quantity        %r8;       #IMPLIED>

<!-- 
	QualityInspectionQuantity is the stock type where the received stock is under quality inspection.
-->
<!ELEMENT QualityInspectionQuantity (UnitOfMeasure?)  >
<!--
    @attr quantity Number of stock items under quality inspection.
-->
<!ATTLIST QualityInspectionQuantity
    quantity        %r8;       #IMPLIED>

<!--
	PromotionQuanity is the stock quantity that is reserved for promotions.
-->
<!ELEMENT PromotionQuantity (UnitOfMeasure?)  >
<!--
    @attr quantity Number of stock items reserved for promotions.
-->
<!ATTLIST PromotionQuantity
    quantity        %r8;       #IMPLIED>
<!--
	When there is a movement of stock between plant to plant, company code to company code then 
	such movement of stock is called as Stock transfer.
-->
<!ELEMENT StockInTransferQuantity (UnitOfMeasure?)  >
<!--
    @attr quantity Number of stock items in transfer from plant to plant.
-->
<!ATTLIST StockInTransferQuantity
    quantity        %r8;       #IMPLIED>

<!-- 
	Quantity used to increment (add to) stock.
-->
<!ELEMENT IncrementQuantity (UnitOfMeasure?)  >
<!--
    @attr quantity Number of stock items to increment.
-->
<!ATTLIST IncrementQuantity
    quantity        %r8;       #IMPLIED>

<!--
	Minimum stock level above which the stock has to be maintained.
-->
<!ELEMENT RequiredMinimumQuantity (UnitOfMeasure?)  >
<!--
    @attr quantity Number of minimum stock items to maintain in inventory.
-->
<!ATTLIST RequiredMinimumQuantity
    quantity        %r8;       #IMPLIED>

<!--
	Maximum stock level above which the stock has to be maintained.
-->
<!ELEMENT RequiredMaximumQuantity (UnitOfMeasure?)  >
<!--
    @attr quantity Number of maximum stock items to maintain in inventory.
-->
<!ATTLIST RequiredMaximumQuantity
    quantity        %r8;       #IMPLIED>

<!--
	Forecast represents the forecast quantity of a product for a specific time period.
-->
<!ELEMENT Forecast (Period, ForecastQuantity, UnitPrice?, Extrinsic*)  >

<!--
	Forecast quantity of a product for a specific time period.
-->
<!ELEMENT ForecastQuantity (UnitOfMeasure?)  >
<!--
    @attr quantity Number of forecast items.
-->
<!ATTLIST ForecastQuantity
    quantity        %r8;       #IMPLIED>

<!--
	The quantity withdrawn in a consignment movement.
-->
<!ELEMENT MovementQuantity (UnitOfMeasure?)  >
<!--
    @attr quantity Number of stock items withdrawn in a consignment movement.
-->
<!ATTLIST MovementQuantity
    quantity        %r8;       #IMPLIED>
    
<!--
	The minimum/maximum order quantity refers to the minimum/maximum quantity the customer must order. The trading 
	partners are alerted if the order quantity is not within the required quantity range during order entry.
-->
<!ELEMENT OrderQuantity (UnitOfMeasure?)>
<!--
    @attr minimum Minimum order quantity.
	
    @attr maximum Maximum order quantity.
-->
<!ATTLIST OrderQuantity
    minimum        %r8;    #IMPLIED
    maximum        %r8;    #IMPLIED>

<!--
	DaysOfSupply is used to determine for how long stocks and receipts will cover the requirements, to avoid 
	product shortages or stock levels that are too high. Minimum and Maximum Days of Supply in days can be 
	specified. The system then issues replenishment proposals when the days’ supply falls above or Below the 
	threshold.
-->
<!ELEMENT DaysOfSupply EMPTY>
<!--
    @attr minimum Minimum days of supply quantity.
	
    @attr maximum Maximum days of supply quantity.
-->
<!ATTLIST DaysOfSupply
    minimum        %r8;    #IMPLIED
    maximum        %r8;    #IMPLIED >

<!--
	Represents the calculated value of different Stock types dependent on the customer, location, and material.
-->
<!ELEMENT StockOnHandQuantity (UnitOfMeasure?)>
<!--
	@attr quantity Number of stock items on hand depending on the customer, location, and material.
-->
<!ATTLIST StockOnHandQuantity
    quantity        %r8;       #REQUIRED >

<!--
	Work in progress (WIP), also called work in process, is inventory that has begun the manufacturing process and 
	is no longer included in raw materials inventory, but is not yet a completed product. On a balance sheet, work 
	in progress is considered to be an asset because money has been spent towards a completed product.
-->
<!ELEMENT WorkInProcessQuantity (UnitOfMeasure?)>
<!--
	@attr quantity Number of work in progress stock items.
-->
<!ATTLIST WorkInProcessQuantity
    quantity        %r8;       #REQUIRED >
    
<!--
	The stock in transit is the quantity of a material that was withdrawn from the stock of the issuing plant 
	but has not yet arrived at the receiving plant.
-->
<!ELEMENT IntransitQuantity (UnitOfMeasure?)>
<!--
	@attr quantity Number of stock items in transit.
-->
<!ATTLIST IntransitQuantity
    quantity        %r8;       #REQUIRED >
    
<!--
	The quantity represents the Scrap of a material that is expected to occur during production if the material is a component.
-->
<!ELEMENT ScrapQuantity (UnitOfMeasure?)>
<!--
	@attr quantity Number of scrapped stock items.
-->
<!ATTLIST ScrapQuantity
    quantity        %r8;       #REQUIRED >
    
<!--
    TimeSeriesDetails carries product activity or product replenishment information regarding the quantity of a product for a specific time period.

    Period: The start date and end date for the forecast response.

    TimeSeriesQuantity: The generic quantity associated with the given type of inventory timeseries or replenishment timeseries.

    TimeSeriesValue: The generic value associated with the timeseries. This can be used for non-numeric values like priority. 
		It is tightly coupled with the keyfigure type specified in the enclosing TimeSeries Element. 

    TimeSeriesAmount: The generic value associated with the timeseries. This can be used to hold any Amount or price based timeseries.

    UpsideQuantity: The quantity of inventory that the supplier can provide above and beyond the request demand.
		This quantity should only be specified when the type of ReplenishmentTimeSeries is forecastConrimation.

    Note: Priority Element is removed and hence please use TimeSeriesValue going forward to map non-numeric values for TimeSeries
-->
<!ELEMENT TimeSeriesDetails (Period, (TimeSeriesQuantity| TimeSeriesValue | TimeSeriesAmount)?, UpsideQuantity?, IdReference*, Extrinsic*)>

<!--
    The generic quantity associated with the given type of inventory timeseries or replenishment timeseries.
-->
<!ELEMENT TimeSeriesQuantity (UnitOfMeasure?)>
<!--
	@attr quantity Number of stock items in the given timeseries.
-->
<!ATTLIST TimeSeriesQuantity
    quantity        %r8;    #REQUIRED>
    
<!--
    The generic value associated with the timeseries. This can be used to hold any non-numeric values associated with timeseries.
-->
<!ELEMENT TimeSeriesValue (Description?)>
<!--
	@attr value Generic value associated with the timeseries. This can be used for non-numeric values like priority. 
		It is tightly coupled with the keyfigure specified in the TimeSeries.type attribute.
-->
<!ATTLIST TimeSeriesValue
    value        %string;    #REQUIRED>

<!--
    The generic value associated with the timeseries. This can be used to hold any Amount or price based timeseries.
-->
<!ELEMENT TimeSeriesAmount (Money)>

<!--
    The Forecast Upside quantity. The quantity of inventory that the supplier can provide above and beyond the request demand.
    This quantity should only be specified when the type of ReplenishmentTimeSeries is forecastConrimation.
-->
<!ELEMENT UpsideQuantity (UnitOfMeasure?)  >
<!--
	@attr quantity Number of stock items the supplier can provide above and beyond the request demand.
-->
<!ATTLIST UpsideQuantity
    quantity        %r8;       #REQUIRED >

<!--
	Defines a quality notification for one or more defects associated with a line item on a purchase order or a ship 
	notice. Buyers or suppliers can send a quality notification to each other. You use a code to classify each item 
	according to the type of problem or defect. The details of the problem or defect can contain causes, tasks, 
	activities, codes, code groups, and descriptions.
	
    Quality Notification Header: The quality notification header contains data that is relevant for the entire 
		quality notification. This includes data about the partners, locations, reference documents, the subject, 
		task, activity, affected product and quantity.

    Defect items: A quality notification can contain one or more defect items. A defect item provides details 
		about a specific problem or defect. You use a code to classify each item according to the type of problem or 
		defect (codes).

    Details of Defect Item: A defect item can contain causes, tasks, activities, including codes, code groups, 
		and description.
-->
<!ELEMENT QualityNotificationRequest (QualityNotificationRequestHeader, QualityNotificationRequestItem*)>

<!--
    Contains information about this quality notification that is common to all contained QualityNotificationRequestItem.
    
    DocumentReference: Reference to an earlier QualityNotificationRequest. If the attribute QualityNotificationRequestHeader&#064;operation 
		is "update", the DocumentReference is required and it must reference the original QualityNotificationRequest document (with 
		value "new" for its attribute QualityNotificationRequest&#064;operation) during the same notification process.
    
	QualityInspectionRequestReference: Reference to the Quality Inspection Request. The reference can contain the payloadId or 
		DocumentNumber and DocumentDate of the Quality Inspection Request. This is used in the case when quality inspection results 
		are not met successfully and a Quality Notification is created with reference to the Quality Inspection Request.

    QNCode: These codes define the quality notification in four domains:
		<ul>
        <li>"type" of quality notification without code group.</li>
        <li>"subject" (category) group and code.</li>
        <li>"reason" of this quality notification.</li>
        <li>"revision" quality notification revision.</li>
		</ul>
		
    ShipTo, BillTo, Shipping: Optional. Addresses related to item if it is not related to referenced purchase order or ship notice.
	
    Contact: Related contacts like buyerParty, sellerParty, senderBusinessSystemID, senderParty, receipientParty or componentSupplier.
	
    QNNotes: Structure to define comments and attachments.
	
    Priority: Priority for quality notification.
	
    RequestedProcessingPeriod: The time period during which the processing is required.
	
    MalfunctionPeriod: The time period during which item presented this defect.
	
    ReferenceDocumentInfo: Optional. Reference to purchase order or ship notice on which the quality notification is reported. 
		Notifications are only addressed to a single item. It is possible send a Quality Notification without a referenced document in which case 
		relevant information should be send in the other elements.
	 
    ItemInfo: To get the goods information.
	
    Batch: Standard element to capture batch information of Customer and Supplier.
	
    ComplainQuantity: Quantity that was complained.
	
    ReturnQuantity: Quantity that was returned.
	
    QualityNotificationTask: Structure to capture task detail at quality notification level: Task Number (Auto Generated 1 to n), 
		Code Group (Task type), Code Group description, Task Code, Task Code Description, Description,  Comments for Task Section,
		Status, start end datetime, Processor and Completion detail.
	
    QualityNotificationActivity: Structure to capture Activity detail at Quality Notification level: Activity Number, Code Group (Activity type)
		Code Group description, Activity Code, Code Description, Description, Comments for activity Section,
		Status, start end datetime.
	
    AssetInfo: The list of asset information of the complaint items.

    Extrinsic: Additional information related to this document.

    QualityNotificationHeaderIndustry: Industry specific header information.
-->
<!ELEMENT QualityNotificationRequestHeader (
    DocumentReference?,
    QualityInspectionRequestReference?,
    QNCode*,
    ShipTo?,
    BillTo?,
    Shipping?,
    Contact*,
    QNNotes+,
    Priority,
    RequestedProcessingPeriod?,
    MalfunctionPeriod?,
    ReferenceDocumentInfo?,
    ItemInfo,
    Batch?,
    ComplainQuantity?,
    ReturnQuantity?,
    QualityNotificationTask*,
    QualityNotificationActivity*,
    AssetInfo*,
    Extrinsic*,
    QualityNotificationHeaderIndustry?)>
<!--
    @attr requestID The network hub document number for the quality notification.
	
    @attr externalRequestID ERP document number for the quality notification.
	
    @attr requestDate Quality notification request date and time.
	
    @attr requestVersion Version number of this document if the current operation is "update".
	
    @attr operation Operation to be performed. Possible values:
        <ul>
		<li>new - Creates a new quality notification.</li>
		<li>update - Updates an existing quality notification. The DocumentReference element references 
			the original quality notification.</li>
		</ul>
		
    @attr status Current status of the document. Possible values: draft, new (default), in-process, completed, 
		postponed, canceled, and closed.
		
    @attr discoveryDate Date and time when the defect was discovered.
	
    @attr serialNumber Serial number of the defective goods.
	
    @attr returnDate Date on which the defective goods were returned.
	
    @attr returnAuthorizationNumber Return Authorization Number information for a line item.
	
    @attr itemCategory Set to "subcontract" if the defect originated with a subcontract supplier.
	
    @attr minimumRequiredTasks Number of minimum required tasks to close this quality notification. Evaluated only 
		when the status is "closed".
		
    @attr minimumRequiredActivities Number of minimum required activities to close this quality notification. 
		Evaluated only when the status is "closed".
		
    @attr minimumRequiredCauses Number of minimum required causes to complete a defect. Evaluated only when this 
		attribute is not present at the defect level.	
-->
<!ATTLIST QualityNotificationRequestHeader
    requestID                 %string;              #REQUIRED
    externalRequestID         %string;              #IMPLIED
    requestDate               %datetime.tz;         #REQUIRED
    requestVersion            %number;              #IMPLIED
    operation                (new | update)         "new"
    status                   (draft | new | in-process | completed | postponed | canceled | closed)  "new"
    discoveryDate             %datetime.tz;         #IMPLIED
    serialNumber              %string;              #IMPLIED
    returnDate                %datetime.tz;         #IMPLIED
    returnAuthorizationNumber %string;              #IMPLIED
    itemCategory             (subcontract)          #IMPLIED
    minimumRequiredTasks	  %uint;                #IMPLIED
    minimumRequiredActivities %uint;                #IMPLIED
    minimumRequiredCauses     %uint;                #IMPLIED  
>

<!--
    Contains defect item detail for a quality notification.

    QNCode: Code with domain "defect" represents the defect's category/subcategory.
		Code with domain "defectLocation" represents the area where the defect occurs.
    
	OwnerInfo: Contains information about the owner of this defect.
    
	Description: Explanatory text describing this defect.

	QNNotes: Structure to define comments and attachments.
	
    Period: Start and end time period during which the processing of this defect is required.
    
	AdditionalQNInfo: Structure to add additional batch and part numbers to this defect.
    
	QualityNotificationTask: Tasks required to process this item.
    
	QualityNotificationActivity: Activities required to process this item.
    
	QualityNotificationCause: Causes of defects in this item.
    
	AssetInfo: The list of asset information of the complaint items.
    
	Extrinsic: Additional information related to this defect.
-->
<!ELEMENT QualityNotificationRequestItem (
    QNCode*,
    OwnerInfo?,
    Description?,
    QNNotes*,
    Period?,
    AdditionalQNInfo*,
    QualityNotificationTask*, 
    QualityNotificationActivity*, 
    QualityNotificationCause*,
    AssetInfo*,
    Extrinsic*)>
<!--
    @attr defectId Number of the defect.

    @attr defectCount Quantity of defects in this quality notification item.

    @attr isCompleted Set to "yes" if all activities and tasks were completed.

    @attr completedDate Date and time when quality notification item was completed.

    @attr minimumRequiredTasks Number of minimum required tasks to complete this defect. Evaluated only when the isCompleted is true.

    @attr minimumRequiredActivities Number of minimum required activities to complete this defect. Evaluated only when the 
		isCompleted is true.

    @attr minimumRequiredCauses Number of minimum required tasks to complete this defect. Evaluated only when the isCompleted is true.
-->
<!ATTLIST QualityNotificationRequestItem
    defectId                     %string;         #REQUIRED
    defectCount                  %uint;           #IMPLIED
    isCompleted                  (yes)            #IMPLIED
    completedDate                %datetime.tz;    #IMPLIED
    minimumRequiredTasks	     %uint;           #IMPLIED
    minimumRequiredActivities    %uint;           #IMPLIED
    minimumRequiredCauses        %uint;           #IMPLIED
>

<!--
    Provides additional information about a quality notification, such as part numbers (customer or supplier), batch information, or other information.

    ItemID: Represents the supplier part number and customer part number. To provide a plant location description, the domain attribute of the 
		element IdReference should be set to "buyerLocationID", "supplierLocationID" or "storageLocation" and the identifier to the plantID.

    Batch: Standard element to capture batch information of Customer and Supplier.
	
    Extrinsic: Additional information related to this element.
-->
<!ELEMENT AdditionalQNInfo (
    ItemID?,
    Batch?,
    Extrinsic*)>
<!--
    @attr lineNumber Represents the AdditionalQNInfo line number.
-->
<!ATTLIST AdditionalQNInfo
    lineNumber              %string;         #REQUIRED
>

<!--
    Quality notification task.

    QNCode: Codes for task with domain "task".

    OwnerInfo: Contains information about the owner of this task.

    Description: Explanatory text describing this task.

    QNNotes: Structure to define comments and attachments.

    Period: Planned start and end time period during which the processing of this task is required.

    Extrinsic: Additional information related to this task.
-->

<!ELEMENT QualityNotificationTask (
    QNCode?,
    OwnerInfo?,
    Description?,
    QNNotes*,
    Period?,
    Extrinsic*)>
<!--
    @attr taskId Number of the task.
	
    @attr status Current state of execution of this task. "Close" status is the final step when a task 
		is considered closed. 
		
    @attr completedDate Date time when task was completed.
	
    @attr completedBy User ID of the person who completed this task.
	
	@attr processorId The ID of the person or organization responsible for this task. In the case of a 
		supplier, can be the user ID or the network hub ID.
		
    @attr processorName The name of the person or organization responsible for this task. In the case of a supplier, 
		can be the user name or company name.
		
    @attr processorType Type of processor.
-->
<!ATTLIST QualityNotificationTask
    taskId               %string;         #REQUIRED
    status               (new | in-process | complete | close)         "new"
    completedDate        %datetime.tz;    #IMPLIED
    completedBy          %string;         #IMPLIED
    processorId          %string;         #IMPLIED
    processorName        %string;         #IMPLIED
    processorType        (customer | supplier | customerUser)  #IMPLIED
>
 
<!--
    Quality notification activity.

    QNCode: Codes for activities with domain "activity".
	
    OwnerInfo: Contains information about the owner of this activity.
    
	Description: Explanatory text describing this activity.

	QNNotes: Structure to define comments and attachments.
    
	Period: Start and end time period during which the processing of this activity is required.
    
	Extrinsic: Additional information related to this activity.
-->
<!ELEMENT QualityNotificationActivity (
    QNCode?,
    OwnerInfo?,
    Description?,
    QNNotes*,
    Period?,
    Extrinsic*)>
<!--
    @attr activityId Number of the activity.
	
    @attr isCompleted Set to "yes" if the activity was completed.
-->
<!ATTLIST QualityNotificationActivity
    activityId         %string;        #REQUIRED
    isCompleted        (yes)           #IMPLIED
>

<!--
    Industry specific header information

    AerospaceAndDefense: Provides Aerospace and Defense Industry information.
-->
<!ELEMENT QualityNotificationHeaderIndustry (AerospaceAndDefense?)>

<!--
    Defines the cause of a quality notification.

    QNCode: Group and codes defining the type of cause.
	
    OwnerInfo: Contains information about the owner of this cause.
	
    Description: Explanatory text describing this cause.

    QNNotes: Structure to define comments and attachments.
	
    Extrinsic: Additional information related to this cause.
-->
<!ELEMENT QualityNotificationCause (
    QNCode?,
    OwnerInfo?,
    Description?,
    QNNotes*,
    Extrinsic*)>
<!--
    @attr causeId Number of the cause.
-->
<!ATTLIST QualityNotificationCause
    causeId          %string;         #REQUIRED
>

<!--
    Support for quality notification codes and their descriptions in different domains.
-->
<!ELEMENT QNCode EMPTY>
<!--
    @attr domain Domain in which this value has meaning. Recognized domains includes:
		<ul>
		<li>type - Quality notification type, does not have code group.</li>
        <li>subject - Quality notification subject (category).</li>
        <li>reason - Reason of defect.</li>
        <li>task - Quality notification task.</li>
        <li>activity - Quality notification activity.</li>
        <li>defect - Quality notification defect.</li>
        <li>defectLocation - Area where a quality notification defect occurs (used in 
			QualityNotificationRequestItem only).</li>
        <li>cause - Quality notification cause.</li>
        <li>revision - Quality notification revision.</li>
		</ul>
		
    @attr codeGroup Name of the code group.
	
    @attr codeGroupDescription Description of the code group.
	
    @attr code Quality notification code.
	
    @attr codeDescription Description of the quality notification code.
-->
<!ATTLIST QNCode
    domain               %string;       #REQUIRED
    codeGroup            %string;       #IMPLIED
    codeGroupDescription %string;       #IMPLIED
    code                 %string;       #REQUIRED
    codeDescription      %string;       #REQUIRED
>

<!--
    Defines subject code, description, and attachments of a quality notification.

    QNCode: Code identifying the type message. Its domain is “subject”.
	
    Description: Detail of the subject.
	
    Attachment: Attached files to this message.
-->
<!ELEMENT QNNotes (QNCode?, Description, Attachment*)>
<!--
    @attr user User name or user ID of the person who created the message.
	
    @attr createDate Timestamp of the message.
-->
<!ATTLIST QNNotes
    user        %string;        #REQUIRED
    createDate  %datetime.tz;   #REQUIRED
>

<!--
    Quantity that was subject to complaints.

    UnitOfMeasure: Unit of measure defined in cXML.dtd.
-->
<!ELEMENT ComplainQuantity (UnitOfMeasure)>
<!--
    @attr quantity Number of items subject to complaints.
-->
<!ATTLIST ComplainQuantity
    quantity    %r8;        #REQUIRED
>

<!--  
    The time period during which the processing of this defect was required.

    Period: Start and end date time
-->
<!ELEMENT RequestedProcessingPeriod (Period)>

<!--  
    The time period during which item presented this defect.

    Period: Start and end date time.
-->
<!ELEMENT MalfunctionPeriod (Period)>

<!--  
    Describes the owner information of a defect, task, cause, or activity.
-->
<!ELEMENT OwnerInfo EMPTY>
<!--
    @attr owner Owner of the element. This attribute should contain the userId

    @attr role Role of the owner. Possible values are customer or supplier.
-->
<!ATTLIST OwnerInfo
    owner   %string;                #REQUIRED
    role    (customer | supplier)   #REQUIRED 
>

<!--
    Request to examine a specific quantity of materials at a plant.

    QualityInspectionRequestHeader: Contains data that is relevant for the entire quality inspection. This 
	includes data about the partners, locations, reference documents, and item information.
	
    QualityInspectionRequestDetail: Detail about quality inspection request.
-->
<!ELEMENT QualityInspectionRequest (QualityInspectionRequestHeader, QualityInspectionRequestDetail)>

<!--
    Contains common information about the quality inspection request.
    
    DocumentReference: Reference to an earlier QualityInspectionRequest. If the attribute QualityInspectionRequestHeader&#064;operation is "update", 
		the DocumentReference is required and it must reference the original QualityInspectionRequest document (with value "new" for 
		its attribute QualityInspectionRequest&#064;operation) during the same quality inspection process.
	
		If the attribute QualityInspectionRequestHeader&#064;operation is "delete", the DocumentReference is required and it must reference the 
		original QualityInspectionRequest document (with value "new" or "update" for its attribute QualityInspectionRequest@operation) during 
		the same quality inspection process.
	
    IdReference: This is the inspection type, composed by code and description.
		Use IdReference element with domain "inspectionType". 

    ShipTo, BillTo: Optional addresses related to item if it is not related to referenced PO or GRN.
	
    Contact: Related contacts like buyerParty, sellerParty, senderBusinessSystemID, senderParty, receipientParty or componentSupplier.

    Period: Start and end date of the quality inspection request.
	
    Priority: Priority for Quality Inspection.
	
    ReferenceDocumentInfo: Reference to Purchase Order or Good Receipt on which the quality inspection request is reported. Quality Inspection 
		request is only addressed to a single item. 

    ItemInfo: To get the good information. Lot quantity for inspection is handled in quantity attribute.
		Use UnitOfMeasure element for the UOM of lot quantity, samples.

    SampleDefinition: This element contains the sample size, only use sampleSize at header level.

    Batch: Standard element to capture batch information of Customer and Supplier.
	
    QualityInfo: The representation of the quality information of a line item.

    AssetInfo: The list of asset information of the complaint items.

    Comments: Comments associated with Quality Inspection Request.

    Extrinsic: Optional additional information related to Quality Inspection Request.

    QualityInspectionHeaderIndustry: Industry specific header information.
-->
<!ELEMENT QualityInspectionRequestHeader (
    DocumentReference?,
    IdReference,
    ShipTo?,
    BillTo?,
    Contact*,
    Period?,
    Priority?,
    ReferenceDocumentInfo?,
    ItemInfo,
    SampleDefinition?,
    Batch?,
    QualityInfo?,
    AssetInfo*,
    Comments?,
    Extrinsic*,
    QualityInspectionHeaderIndustry?)>
<!--
    @attr requestID Network hub document number for the quality inspection request.

    @attr requestDate Quality inspection request date and time.

    @attr operation Operation to be performed. Possible values:
		<ul>
		<li>new (default) - Creates a new quality inspection request.</li>
        <li>update - Updates an existing quality inspection request.</li> 
        <li>delete - Cancels an existing quality inspection request.</li>
		</ul>
        For update and delete operations, the DocumentReference element should indicate the original quality inspection request.
		
    @attr version Version number of this document if the current operation is update. The version of a new 
		QualityInspectionRequest is 1. An update would increment the version to 2, 3, 4, and so on.
	
    @attr createdBy User who created the quality inspection request.
-->
<!ATTLIST QualityInspectionRequestHeader
    requestID               %string;            #REQUIRED
    requestDate             %datetime.tz;       #REQUIRED
    operation               (new | update | delete)      "new"
    version                 %uint;              #IMPLIED
    createdBy               %string;            #IMPLIED
>

<!--
    Details about a quality inspection request.

    QualityInspectionCharacteristic: Represents an inspection characteristic that describes what should be inspected.
		A quality inspection request can contain one or more characteristics.
-->
<!ELEMENT QualityInspectionRequestDetail (QualityInspectionCharacteristic+)>

<!--  
    Represents an inspection characteristic that describes what should be inspected.

    Description: Quality Inspection Request description.
	
    IdReference: Describes the code and description of the buyer characteristic, supplier characteristic, inspection 
		method and additional information.
        For customer characteristic, the domain is "buyerInspectionCode".
        For supplier characteristic, the domain is "supplierInspectionCode".
        For inspection method, the domain is "inspectionMethod". 
        For additional information, the domain is "additionalInfo".
			
    AllowedValues: Allowed values during the valuation.
	
    ExpectedResult: Expected result for this characteristic.        
        This element is necessary because is used in UI to calculate the conformance and non-conformance samples by characteristic.
		
    SampleDefinition: This element contains the sample size and sample type.
	
    Comments: Additional text.
	
    Extrinsic: Optional additional information related to this document.     
-->
<!ELEMENT QualityInspectionCharacteristic (
    Description?,
    IdReference*,
    AllowedValues,
    ExpectedResult?,
    SampleDefinition?,
    Comments?,
    Extrinsic*
    )>
<!--
    @attr characteristicID Number of the characteristic. The combination of this attribute with operationNumber should be unique.

    @attr operationNumber Inspection operation. The combination of this attribute with characteristicID should be unique.

    @attr workCenter Work center number.

    @attr procedure A text value that specifies how the sample size is calculated and how the inspection characteristic is assigned values. 

    @attr isLocked Set to "yes" to indicate that a characteristic is locked and no further valuations are allowed.

    @attr allowDefectRecording Set to "yes" to indicate that defect recording is allowed. If an inspection characteristic is rejected 
		during results recording, the function for recording defects for a characteristic is automatically invoked.

    @attr characteristicType A buyer uses inspection characteristics to describe the inspection criteria for materials, parts, and 
		products. This allows the buyer to plan inspections systematically, uniformly, and economically. An inspection request can 
		contain different types of characteristics, some required, optional, or conditional. Specify one of the following values:
		<ul>
		<li>required - The inspection of this characteristic is required to create an inspection decision.</li>
		<li>optional - The inspection of this characteristic is not required to create an inspection decision.</li>
		<li>afterAccept - This conditional inspection characteristic must be inspected if the previous required characteristic was accepted.</li>
		<li>afterRejection - This conditional inspection characteristic must be inspected if the previous required characteristic was rejected.</li>
		</ul>

    @attr isQuantitative Set to "yes" if this characteristic uses a numerical measurement. Otherwise, the characteristic uses a non-numerical, 
		qualitative measurement.

    @attr recordingType Indicates how the valuation will be sent for this characteristic: 
        <ul>
		<li>singleResult - Single values for each inspection characteristic.</li>
		<li>summarizedRecording - Summarized values for each inspection characteristic.</li>
		<li>noCharacteristicRecording - No characteristic record.</li>
		<li>classedRecording - Classed values for each inspection characteristic.</li>
		</ul>

    @attr expirationDate Expiration date of the characteristic.

    @attr inspectionPoint Inspection point types for equipment, functional locations, physical samples, or production. These inspection 
		point types can be assigned individually in the user specifications, if required.

    @attr version Version of characteristic. If any of the attributes change for the characteristic, the ERP generates another version for it.

    @attr isAdHoc Indicates whether the characteristic does not exist in the reference document and is an additional valuation.
-->
<!ATTLIST QualityInspectionCharacteristic
    characteristicID         %string;          #REQUIRED
    operationNumber          %uint;            #REQUIRED   
    workCenter               %string;          #IMPLIED   
    procedure                %string;          #IMPLIED   
    isLocked                 (yes)             #IMPLIED
    allowDefectRecording     (yes)             #IMPLIED
    characteristicType       (required | optional | afterAccept | afterRejection)      #IMPLIED  
    isQuantitative           (yes)             #IMPLIED
    recordingType            (summarizedRecording | singleResult | noCharacteristicRecording | classedRecording)  #IMPLIED    
    expirationDate           %datetime.tz;     #IMPLIED 
    inspectionPoint          %string;          #IMPLIED
    version                  %number;          #IMPLIED
    isAdHoc                  (yes)             #IMPLIED
>

<!--
    Sample definition.
-->
<!ELEMENT SampleDefinition EMPTY>
<!--
    @attr sampleSize Number of the samples required.

    @attr sampleType Sample type. Possible values are:
		<ul>
        <li>samplingProcedure - A sampling procedure must be assigned when the master inspection characteristic is used in the 
			task list or in the material specification (optional for an inspection with a material specification).</li>
        <li>additiveSample - When samples are calculated, the size of the partial sample is increased by the quantity that is 
			required for the inspection of this characteristic.</li>
        <li>destructiveSample - The sample should be destroyed after inspection. This means that it is no longer available. 
			This quantity is used in the usage decision as the proposed quantity for the posting to sample.</li>
        <li>spcCharacteristic - A statistical process control chart should be run.</li>
		</ul>
-->
<!ATTLIST SampleDefinition 
    sampleSize               %uint;            #IMPLIED
    sampleType               (samplingProcedure | additiveSample | destructiveSample | spcCharacteristic) #IMPLIED
>

<!--
    Industry specific header information for Quality Inspection Request.

    AerospaceAndDefense: Provides Aerospace and Defense Industry information.
-->
<!ELEMENT QualityInspectionHeaderIndustry (AerospaceAndDefense?)>

<!--
    Allowed values during the valuation.

    PropertyValue: List of available values. This is mandatory when "choice" is selected for type attribute.
		This element is used also to provide the unit of measure of the allowed values by characteristic using 
		PropertyValue&#064;Characteristic.
-->
<!ELEMENT AllowedValues (PropertyValue?)>
<!--
    @attr type Type of allowed values. Possible values:
		<ul>
		<li>numeric - Only numeric values are allowed during the valuation.</li>
		<li>decision - Only "Accept" and "Reject" values are allowed during the valuation.</li>
		<li>choice - The provided values in "PropertyValue" element are allowed during the valuation.</li>
		</ul>
-->
<!ATTLIST AllowedValues     
    type         (numeric | decision | choice)      #IMPLIED
>

<!--
    Expected result for a measurement.

    MinimumLimit: Minimum acceptable value.
	
    MaximumLimit: Maximum acceptable value.
	
    PropertyValue: This is used to send the expected result for qualitative valuation.
-->
<!ELEMENT ExpectedResult (
    MinimumLimit?,
    MaximumLimit?,
    PropertyValue?)>
<!--
    @attr targetValue Target value for the measurement.
	
    @attr valuePrecision Integer specifying the number of decimal places to include in the result.
	
    @attr qualitativeValue A non-numerical value (a string) to send the expected result for qualitative valuation.
-->
<!ATTLIST ExpectedResult 
    targetValue        %r8;              #IMPLIED
    valuePrecision     %uint;            #IMPLIED
    qualitativeValue   %string;          #IMPLIED
>

<!--
    Minimum acceptable value.
-->
<!ELEMENT MinimumLimit (ComparatorInfo)>

<!--
    Maximum acceptable value.
-->
<!ELEMENT MaximumLimit (ComparatorInfo)>

<!--
    Structure to support comparison regarding a value.
-->
<!ELEMENT ComparatorInfo EMPTY>
<!--
    @attr comparatorType Type of comparison.

    @attr comparatorValue Reference value to make the comparison.
-->
<!ATTLIST ComparatorInfo 
    comparatorType     (greater | less | greaterOrEqual | lessOrEqual)  #REQUIRED
    comparatorValue    %r8;              #REQUIRED
>

<!--
    This is the supplier result for quality inspection request, it contains all the valuation according characteristic specification in request.

    QualityInspectionResultRequestHeader: Contains common information about this Quality Inspection Result.

    QualityInspectionValuation: To confirm the characteristic inspection results recorded in different inspection operations.      
-->
<!ELEMENT QualityInspectionResultRequest (QualityInspectionResultRequestHeader, QualityInspectionResultRequestDetail)>

<!--  
    Contains common information about this Quality Inspection Result.

    QualityInspectionRequestReference: Reference to the Quality Inspection Request. The reference can contain 
		the payloadId or DocumentNumber and DocumentDate of the Quality Inspection Request.
    
	Batch: Standard element to capture batch information of Customer and Supplier.
    
	Comments: Comments associated with Quality Inspection Result, including one by attachment. If a Comments element includes 
		an attached document, the Comments&#064;type attribute supports only "CertificateOfAnalysis" and "CertificateOfConformance". 
		For other document types, do not send the type.
    
	QualityInspectionQuantity: Inspection lot quantity could be filled by supplier based on the inspected quantity on 
		QualityInspectionResult. Generally, it is send by the buyer but if not contract manufacturer based on the 
		inspection lot can provide this quantity.
    
	AssetInfo: The list of asset information of the valuated items.
    
	Extrinsic: Optional additional information related to Quality Inspection Result. 
-->
<!ELEMENT QualityInspectionResultRequestHeader (
    QualityInspectionRequestReference,
    Batch*,
    Comments*,
    QualityInspectionQuantity?,
    AssetInfo*,
    Extrinsic*)>
<!--
    @attr resultID Network hub document number for the quality inspection result.

    @attr resultDate Quality inspection result date and time.

    @attr version Version of the document.

    @attr createdBy User who created the quality inspection result.
-->
<!ATTLIST QualityInspectionResultRequestHeader
    resultID              %string;            #REQUIRED
    resultDate            %datetime.tz;       #REQUIRED
    version               %number;            #IMPLIED
    createdBy             %string;            #IMPLIED
>

<!--
    Details about quality inspection result.

    QualityInspectionValuation: Contains the valuation of a characteristic of an inspection request.
-->
<!ELEMENT QualityInspectionResultRequestDetail (QualityInspectionValuation+)>

<!--
    Contains the valuation of a characteristic specified in an inspection request.

    QualitySampleResult: Valuation for each sample inspected. When QualityInspectionCharacteristic&#064;recordingType is "summarizedRecording" 
	and AllowedValues@type is "decision" or "choice" the summarized valuation is sent in this element. When QualityInspectionCharacteristic&#064;recordingType 
	is "summarizedRecording" and AllowedValues&#064;type is "numeric" this element is not required.

    ValueGroup: Structure to support codes and code groups for this valuation.

    Description: Valuation description. 
	
    Extrinsic: Optional additional information related to this valuation. 
-->
<!ELEMENT QualityInspectionValuation (
    QualitySampleResult*,
    ValueGroup*,                  
    Description?,  
    Extrinsic*
    )>
<!--
    @attr valuationID Number of the valuation.

    @attr characteristicID Characteristic ID number. The combination of this attribute with operationNumber should be unique.

    @attr operationNumber Inspection operation number. The combination of this attribute with characteristicID should be unique.

    @attr workCenter Work center number.

    @attr meanValue Mean value in the valuation.

    @attr aboveTolerance Number of samples above tolerance limit.

    @attr belowTolerance Number of samples below tolerance limit. 

    @attr inspectedQuantity Number of samples inspected.

    @attr nonConformance Total number of samples above and below tolerance limits. This value is auto-calculated when suppliers 
		specify the aboveTolerance or belowTolerance values. Otherwise, suppliers can specify the nonConformance value.

    @attr deviation Standard deviation value.

    @attr variance Variance of valid measured values for this characteristic. Applies to calculated characteristics for 
		which the valuation is based on a formula.

    @attr numberOfDefects Number of the defects in this valuation.

    @attr serialNumber Single unit number for the unit to be inspected.

    @attr inspectionDate Date of the characteristic inspection.

    @attr isAdHoc Indicates whether the characteristic does not exist in the reference document and is an additional valuation..
-->
<!ATTLIST QualityInspectionValuation
    valuationID                 %uint;            #REQUIRED
    characteristicID            %uint;            #REQUIRED
    operationNumber             %uint;            #REQUIRED
    workCenter                  %string;          #IMPLIED
    meanValue                   %r8;              #IMPLIED   
    aboveTolerance              %r8;              #IMPLIED 
    belowTolerance              %r8;              #IMPLIED   
    inspectedQuantity           %uint;            #IMPLIED
    nonConformance              %r8;              #IMPLIED     
    deviation                   %string;          #IMPLIED
    variance                    %string;          #IMPLIED     
    numberOfDefects             %r8;              #IMPLIED
    serialNumber                %string;          #IMPLIED     
    inspectionDate              %datetime.tz;     #IMPLIED
    isAdHoc                     (yes)             #IMPLIED
>

<!--  
    Contains the sample result after valuation. 
    
    PropertyValue: Result of the valuation when AllowedValues&#064;type is "choice" in QualityInspectionCharacteristic.
		This element is used also to provide the unit of measure of the valuation using PropertyValue&#064;Characteristic.
-->
<!ELEMENT QualitySampleResult (PropertyValue?)>
<!--
    @attr sampleID Sample Number in valuation.

    @attr unitValue Value of the sample.

    @attr physicalSampleNumber Unique number for the sample when the sample was taken.
-->
<!ATTLIST QualitySampleResult
    sampleID                    %uint;              #IMPLIED
    unitValue                   %string;            #IMPLIED
    physicalSampleNumber        %uint;              #IMPLIED
>

<!--  
    Represents the posting of stock quantities from an inspection lot within the Quality Management module. An inspection 
	lot is a formal request to examine a specific quantity of materials at a plant.
    
    UnrestrictedUseQuantity: Unrestricted Stock is the physical stock that is always available at a plant/storage location 
	that can be consumed for stock movements and available for Material requirements planning.
	
    ScrapQuantity: The quantity represents the Scrap of a material that is expected to occur during production if the 
	material is a component.
	
    SampleUsageQuantity: Quantity for sampleUsageQuantity.
	
    BlockedQuantity: Blocked stock is not counted as unrestricted stock. You can have Plant level configuration to consider 
	the stock for MRP.
    
	NewMaterialQuantity: For new material quantity
    
	ReserveQuantity: For reserve quantity.
	
    ReturnQuantity: Quantity that was returned.
-->
<!ELEMENT QualityInspectionLotStock (
    UnrestrictedUseQuantity?, 
    ScrapQuantity?,
    SampleUsageQuantity?,
    BlockedQuantity?,
    NewMaterialQuantity?,
    ReserveQuantity?,
    ReturnQuantity?
    )>

<!--
	The quantity of stock to post as sample usage stock.
-->
<!ELEMENT SampleUsageQuantity (UnitOfMeasure?)>
<!--
	@attr quantity Number of items of sample usage stock.
-->	
<!ATTLIST SampleUsageQuantity
    quantity        %r8;       #IMPLIED>

<!--
	The quantity of new material. 
-->
<!ELEMENT NewMaterialQuantity (UnitOfMeasure?)>
<!--
	@attr quantity Number of items of new material.
-->
<!ATTLIST NewMaterialQuantity
    quantity        %r8;       #IMPLIED>

<!--
	The quantity of items kept in reserve. 
-->
<!ELEMENT ReserveQuantity (UnitOfMeasure?)>
<!--
	@attr quantity Number of items kept in reserve.
-->
<!ATTLIST ReserveQuantity
    quantity        %r8;       #IMPLIED>

<!--
    Request to confirm that all physical samples have been valuated and the inspection has been completed.
-->
<!ELEMENT QualityInspectionDecisionRequest (QualityInspectionDecisionDetail)>

<!--  
    Contains the information of the quality inspection decision, like inspection result reference, quality quantity details and the decision of the inspection.
    
    QualityInspectionResultReference: Reference to the inspection result to confirm that all physical samples have been 
	valuated and the inspection has been completed. The reference can contain the payloadId or DocumentNumber and DocumentDate of the Inspection Result.
    
	QualityInspectionRequestReference: Reference to the Quality Inspection Request. The reference can contain the payloadId or 
	DocumentNumber and DocumentDate of the Quality Inspection Request.
    
	ShipNoticeReference: Reference to the Ship Notice Request. The reference can contain the payloadId or DocumentNumber and DocumentDate 
	of the Ship Notice Request.
    
	ReceiptReference: Reference to the Receipt Request. The reference can contain the payloadId or DocumentNumber and DocumentDate of the 
	Receipt Request.
    
	QualityInspectionLotStock: Contains information about what happened to the stock posting. For example, it could include the scrap 
	quantity or the unrestricted use quantity from the ERP.
    
	ValueGroup: Structure to support codes and code groups for this Inspection Decision.
    
	Description: Description for the Usage Decision.
    
	Extrinsic: Optional additional information.
-->
<!ELEMENT QualityInspectionDecisionDetail (
    (QualityInspectionResultReference | QualityInspectionRequestReference | ShipNoticeReference | ReceiptReference),  
    QualityInspectionLotStock, 
    ValueGroup*, 
    Description?,
    Extrinsic*)>
<!--
    @attr decisionID Network hub document number for the quality inspection decision request.

    @attr decisionDate Quality inspection decision request date and time.

    @attr status Usage decision status. Possible values are "accepted" or "rejected".

    @attr qualityScore Quality score for this inspection decision.

    @attr createdBy The user who created the quality inspection decision request.
-->
<!ATTLIST QualityInspectionDecisionDetail
    decisionID               %string;            #REQUIRED
    decisionDate             %datetime.tz;       #REQUIRED
    status                   (accepted | rejected)   #IMPLIED
    qualityScore             %uint;                  #IMPLIED        
    createdBy                %string;                #IMPLIED
>

<!--
	Contains a unique identification of a production facility leveraged during production, such as a piece of equipment or a production line.

	ResourceType: Identifies the resource category the resourceID belongs to. 
  
	InputItemID: Uniquely identifies materials consumed by a resource during production process.
  
	OutputItemID: Uniquely identifies materials produced by a resource during production process.
-->
<!ELEMENT ResourceID (SupplierResourceID, ResourceType?, InputItemID?, OutputItemID?, IdReference*)>

<!--
	Uniquely identifies a plant, piece of equipment, or other production facilities leveraged by the supplier during production. 
-->
<!ELEMENT SupplierResourceID (#PCDATA)>

<!--
	Identifies the resource category the resourceID belongs to. 
-->
<!ELEMENT ResourceType (#PCDATA)>


<!--
	Uniquely identifies materials consumed by a resource during production process.
-->
<!ELEMENT InputItemID ( ItemID*)>

<!--
	Uniquely identifies materials produced by a resource during production process.
-->
<!ELEMENT OutputItemID ( ItemID*)>



