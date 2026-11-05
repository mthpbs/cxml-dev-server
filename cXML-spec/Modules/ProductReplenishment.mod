<!--
    This document is intended for Suppliers to communicate the following types of messages to Buyers:
	<ul>
	<li>For Suppliers to Communicate the Manufacturing and Planning related information to Buyers. The processes 
		for Outsourced Manufacturing processes where Contract Manufacturer is the considered to the extension of 
		internal manufacturing.</li>
	<li>For Suppliers to Communicate the Inventory details to Buyers. The processes for Outsourced Manufacturing 
		and Supplier Managed Inventory requires Suppliers provide customers with Inventory updates to support Planning.</li>
	<li>For Supplier to provide Forecast confirmation to the Buyers in Collaborative Planning processes. The goal 
		of sending a Forecast confirmation is to enable an automated process that provides the receiver critical 
		information about the supplier’s constraints. Forecast Confirmation should help automate existing processes 
		for both senders and receivers.</li>
	</uL>
-->
<!ELEMENT ProductReplenishmentMessage (ProductReplenishmentHeader, ProductReplenishmentDetails+, Extrinsic*)>

<!--
    Contains the product replenishment header.
-->
<!ELEMENT ProductReplenishmentHeader EMPTY>
<!--
    @attr messageID Identifier for this product replenishment message.
	
    @attr creationDate Date and time this product replenishment message was created.
	
    @attr processType Identifies the business process supported by the message. Possible values:
		<ul>
		<li>SMI - Supplier-managed inventory (Inventory, ConsignmentInventory, PlanningTimeSeries, InventoryTimeSeries).</li>
		<li>OEM - OEM-owned inventory scenarios (Inventory, ConsignmentInventory, TimeSeries, PlanningTimeSeries, InventoryTimeSeries).</li>
		<li>VMI - Vendor-managed inventory (Inventory, ConsignmentInventory, PlanningTimeSeries, InventoryTimeSeries).</li>
		<li>3PL - Third-party logistics inventory scenarios (Inventory, ConsignmentInventory, PlanningTimeSeries, InventoryTimeSeries).</li>
		<li>ManufacturingVisibility - Contract manufacturers sharing inventory visibility (Inventory, ConsignmentInventory, 
			PlanningTimeSeries, InventoryTimeSeries).</li>
		<li>Forecast - Forecast collaboration (TimeSeries, PlanningTimeSeries).</li>
		<li>Consignment - Consignment material movements (ConsignmentInventory, ConsignmentMovement).</li>
		<li>Sales - Sales report visibility (SalesReport).</li>
		<li>POC - Purchase order collaboration scenarios (Inventory, ConsignmentInventory, TimeSeries, PlanningTimeSeries).</li>
		<li>Capacity - Capacity planning scenarios (PlanningTimeSeries).</li>
		<li>Other - Other collaboration scenarios.</li>
		</ul>        
-->
<!ATTLIST ProductReplenishmentHeader
     messageID           %string;              #REQUIRED
     creationDate           %datetime.tz;         #IMPLIED
     processType  (SMI|OEM|VMI|3PL|ManufacturingVisibility|Forecast|Consignment|Sales|POC|Capacity|Other )  #IMPLIED>

<!--
    The representation of product replenishment for the given product.
	
    ItemID: A unique identification of a component item in supplier backend system or buyer backend system.
	
    ResourceID: A unique identification of a production facility leveraged during production, such as a piece of equipment 
		or a production line.

    Description: Textual description of the component.

    LeadTime: Lead time in days.

    PlannedAcceptanceDays: Number of days the buyer schedules for the inspection of goods after receiving them.

    ManufacturerPartID: ID with which the item's manufacturer identifies the item.
    
    ManufacturerName: Name of the item's manufacturer.

    ReferenceDocumentInfo: Contains details of referenced document.

    Characteristic: Contains detailed information about an item that can be used across different industries.
        Can also be used to send material characteristics to support configurable material process.

    Batch: Batch information of goods or material. The information includes ID and characteristics.

    Contact: When the type of message is forecast confirmation:
		Contact represents the location to which the product forecast confirmation is taking place.
        The only Contact role (Contact@role attribute value) likely to be sensible in this element are
        "locationTo". To provide a plant location description, the domain attribute of the element IdReference 
		should be set to "buyerLocationID" and the identifier to the plantID.
		
        When the type of message is NOT forecast confirmation:
		<ul>
		<li>Contact with role “locationTo” represents the destination of the product replenishment.</li>
		<li>Contact with role “locationFrom” represents the source of the product replenishment.</li>
		<li>Contact with role “inventoryOwner” (if specified), represents the owner of the inventory, in this case,
			Inventory or ConsignmentInventory should be specified in the message.</li>
		</ul>

	Unit Price: Price per unit of item.

    Comments: These are comments provided for the part whose quantity is being committed.

    ReplenishmentTimeSeries: It represents the product replenishment quantity of a product for a specific time period.

	UnitOfMeasure: The unit of measure for an inventory/forecast/consignment quantity at the Item level. This will 
		be applicable for the entire item and will be defaulted over the unit of measure maintained across the 
		time series quantities.
-->
<!ELEMENT ProductReplenishmentDetails ((ItemID| ResourceID),   Description?, LeadTime?, PlannedAcceptanceDays?, ManufacturerPartID?,
          ManufacturerName?, ReferenceDocumentInfo*, Characteristic*, Batch?, Contact*, UnitPrice?, Inventory?, ConsignmentInventory?, 
          ReplenishmentTimeSeries*, Comments?, UnitOfMeasure?, Extrinsic*)>

<!--
    Contains the product replenishment quantity of a product for a specific time period.

	TimeSeriesDetails: Carries product activity or product replenishment information regarding the quantity of a product for a 
		specific time period.
-->
<!ELEMENT ReplenishmentTimeSeries (TimeSeriesDetails+)>
<!--
	@attr type Type of replenishment order. Possible values:
		<ul>
		<li>manufacturingOrder - An order that initiates the manufacturing process to track the status of manufacturing from 
			Raw Material to Finished Goods status.</li>
		<li>purchaseOrder - Subcontracting purchase order from the backend ERP system.</li>
		<li>supplierForecast - Forecast created by the supplier based on the Demand Supply situation in the backend ERP system. 
			Typically, the supplier creates the forecast for components based on the Finished Goods Demand.</li>
		<li>shipment - Supplier shipment quantity from supplier's location and customer's location to ship the quantities 
			requested.</li>
		<li>projectedStock - Stock that is expected to be available in the location at the end of this day.</li>
		<li>firmReceipt - Total quantity that the supplier wants to deliver to the customer in period so that the total demand 
			from the customer is covered.</li>
		<li>plannedReceipt - Total quantity that the supplier wants to deliver to the customer in a period so that the raw net 
			demands of the buyer are covered. It is a planned quantity that is subject to changes.</li>
		<li>forecastConfirmation - Total quantity that the supplier wants to deliver to the customer in the period so that the 
			total demand from the customer is covered.</li>
		<li>capacityConfirmation - Quantity of production output of a specific resource that supplier commits to attain.</li>
		<li>capacityShiftRuns - Number of shifts a specific production resource can run.</li>
		</ul>
	
	@attr customType String to include buyer-defined custom types, for example, "BuildQuantity".
-->
<!ATTLIST ReplenishmentTimeSeries
type                 (manufacturingOrder | purchaseOrder | supplierForecast | shipment | projectedStock | firmReceipt | plannedReceipt | forecastConfirmation | capacityConfirmation | capacityShiftRuns | custom)  #REQUIRED 
customType %string;   #IMPLIED >
