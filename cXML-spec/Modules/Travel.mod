<!--
    For cXML license agreement information, please see
    http://www.cxml.org/home/license.asp

    $Id$
-->

<!--
    Captures travel information. It could be one of four
    types: air, car rental, hotel, or rail.
	
	AirDetail: Air travel detail.

    CarRentalDetail: Car rental detail.

    HotelDetail: Hotel detail.

    RailDetail: Rail detail.

    PolicyViolation: List of policy violations (if any) associated with this
        travel line item.

    Comments: Top level comments for this travel line item (if any). This
        is not the policy violation comment but a top level comment
        given by the user for this travel line item while in Travel
        Booking Provider's web site.

    TermsAndConditions: List of Terms and conditions (if any) associated with this
        travel line item.
-->
<!ELEMENT TravelDetail ((AirDetail | CarRentalDetail | HotelDetail | RailDetail),
                          PolicyViolation*,
                          Comments?,
                          TermsAndConditions?)>
<!--
	@attr confirmationNumber A unique confirmation number that is useful to both the traveler 
		and the vendor who is providing the service for this travel line item. For example, 
		hotel reservation number or e-ticket number from the airline.

	@attr pnrLocator Passenger Name Record (PNR) Locator that is useful to the Travel Booking 
		Provider.

	@attr quoteExpirationTime Date and time that this quote will expire. This value is normally 
		supplied in the PunchoutOrderMessage. If no value is supplied, it is assumed that there 
		is no expiration time or date for this quote.
-->
<!ATTLIST TravelDetail
    confirmationNumber  %string;        #REQUIRED
    pnrLocator          %string;        #IMPLIED
    quoteExpirationTime %datetime.tz;   #IMPLIED
>

<!-- Air -->

<!--
    Air detail information for the air trip.
	
	TripType: Round Trip, One Way, or Multi Leg

    AirLeg: Different air leg that makes up this air detail.
        For example a round trip from SFO -> TPE with no stops
        will have two air legs. One air leg from SFO -> TPE
        and another from TPE -> SFO.

    AvailablePrice: Other available airfare prices that the user did not pick.

    Penalty: Penalty amount (if any). This is normally due to changes or
        cancellation of the ticket.
-->
<!ELEMENT AirDetail (TripType,
                     AirLeg+,
                     AvailablePrice*,
                     Penalty?)>

<!--
    Define a single leg in the air travel.
	
    Vendor: Airline vendor name and information.

    AirLegOrigin: Originating airport for this air leg.

    AirLegDestination: Destination airport for this air leg.

    BookingClassCode: Airline booking class code. This is the de-facto
        airline standard. For example,

        F, FN, P, R, A      - first class
        C, CN, D, J, I, Z   - business class.
        Y, YN, B, BN, M, H, V, VN, O, Q, QN, S, K, KN, L, U, T, W - coach class.

    Rate: Rate for this particular air leg. If specify, the total
        of all the rate for the different air legs must add up
        to the total the line item level.

    Meal: Meal information for this air leg (if any).
-->
<!ELEMENT AirLeg (Vendor,
                  AirLegOrigin,
                  AirLegDestination,
                  BookingClassCode?,
                  Rate?,
                  Meal*)>
<!--
    @attr travelSegment Textual information to identify this travel segment. This information is specific to the Travel Booking Provider.

    @attr departureTime Departure date and time for this air leg.

    @attr arrivalTime Arrival date and time for this air leg.

    @attr flightNumber Flight number for this air leg.

    @attr seatNumber Seat number for this air leg.

    @attr seatType Seat type.

    @attr upgrade Set to "yes" to indicate the ticket is an upgrade.

    @attr stops The number of stop for this air leg. 0 if it is a direct flight. If no information is supplied, it is defaulted to 0.

    @attr equipment The plane equipment information for this air leg.
-->
<!ATTLIST AirLeg
    travelSegment   %string;        #REQUIRED
    departureTime   %datetime.tz;   #REQUIRED
    arrivalTime     %datetime.tz;   #REQUIRED
    flightNumber    %string;        #REQUIRED
    seatNumber      %string;        #IMPLIED
    seatType        (window | aisle | middle) #IMPLIED
    upgrade         (yes)           #IMPLIED
    stops           %r8;            #IMPLIED
    equipment       %string;        #IMPLIED
>

<!--
    Originating airport for this Air Leg.

    Airport: Originating airport.	
-->
<!ELEMENT AirLegOrigin (Airport)>

<!--
    Destination airport for this Air Leg.
	
	Airport: Destination airport.
-->
<!ELEMENT AirLegDestination (Airport)>

<!--
    Airport information that includes the three-letter IATA airport code.
	
    Address: Physical address of the airport.
-->
<!ELEMENT Airport (Address?)>
<!--
	@attr airportCode The three-letter IATA airport code.
-->
<!ATTLIST Airport airportCode %string; #REQUIRED>

<!---
    Meal information used by air, hotel and rail.

    BookingClassCode: Code for the meal. For example, airlines use
		<ul>
		<li>B - Breakfast</li>
        <li>C - Complimentary liquor</li>
        <li>D - Dinner</li>
        <li>F - Food for purchase</li>
        <li>G - Food and beverage for purchase</li>
        <li>H - Hot meal</li>
        <li>K - Continental breakfast</li>
        <li>L - Lunch</li>
        <li>M - Meal</li>
        <li>N - No meal service</li> 
        <li>O - Cold meal</li>
        <li>P - Liquor for purchase</li>
        <li>R - Refreshments</li>
        <li>S - Snack or brunch</li>
        <li>V - Refreshments for purchase</li>
		</ul>

    Description: Textual description of the meal, including any special needs
        such as vegetarian or dairy-free.	
-->
<!ELEMENT Meal (BookingClassCode?, Description?)>

<!-- Car Rental -->

<!--
    Car rental information.

    Vendor: Car rental vendor information.

    CarRentalPickup: Pickup location for the rental car.

    CarRentalDropoff: Drop off location for the rental car.
	
    BookingClassCode: Four-letter code for car.
		<ul>
		<li>1st Letter - M (Mini), E (Economy), C (Compact), S (Standard),
			I (Intermediate), F (Full size), P (Premium), L (Luxury)
            V (MiniVan), X (Special)</li>
        <li>2nd Letter - B (2 door), C (2/4 door), D (4 door), T (Convertible),
			F (Four wheel drive), V (Van), W (Wagon), S (Sport)
            X (Special)</li>
        <li>3rd Letter - A (Automatic), M (Manual)</li>
        <li>4th Letter - R (A/c), N (No A/C)</li>
		</ul>

    CarRentalFee: Multiple car rental fee can be specified to capture the
        break down of different fees. The total of these fees must
        add up to the total at the line item level.

    LimitedMileage: Mileage limit information.

    AvailablePrice: Other available prices for car rental that the user did not pick.
-->
<!ELEMENT CarRentalDetail (Vendor,
                           CarRentalPickup,
                           CarRentalDropoff,
                           BookingClassCode?,
                           CarRentalFee+,
                           LimitedMileage?,
                           AvailablePrice*)>
<!--
	@attr travelSegment Textual information to identify this travel segment. This information is 
		specific to the Travel Booking Provider.

	@attr pickupTime The intended pickup date and time.

	@attr dropoffTime The intended drop off date and time. 
-->
<!ATTLIST CarRentalDetail
    travelSegment %string;          #REQUIRED
    pickupTime    %datetime.tz; 	#REQUIRED
    dropoffTime   %datetime.tz; 	#REQUIRED
>

<!--
    Physical location where the rental car should be picked up. This
    is either an Airport or off airport car rental location.
	
    Airport: An airport location.

    Address: Physical address of the car rental location
-->
<!ELEMENT CarRentalPickup (Airport | Address)>

<!--
    Physical location where the rental car should be dropped off.
    This is either an Airport or off-airport car rental location.
	
    Airport: An airport location.

    Address: Physical address of the car rental location.
-->
<!ELEMENT CarRentalDropoff (Airport | Address)>

<!--
    This specifies the quantity and the unit of measure of the mileage
    limit.
	
    UnitOfMeasure: Unit of measure either miles or kilometers.
-->
<!ELEMENT LimitedMileage (UnitOfMeasure)>
<!-- 
	@attr quantity The mileage limit amount.
-->
<!ATTLIST LimitedMileage
    quantity   %r8;      #REQUIRED
>

<!--
    Car rental fee information. CarRentalFee captures the actual
    charges and fee that applies to this rental. Conditional charges
    such as extra mileages that are over the mileage limit should not
    be specified here but rather in the TermsAndConditions text.
	
    Total: Total amount for this car rental fee. All the total for the rates
        must add up to this amount.

    Rate: The individual broken-down fee information.
-->
<!ELEMENT CarRentalFee (Total, Rate*)>
<!--
	@attr type Type of rate. Possible values:
		<ul>
		<li>baseRate - Base rental rate.</li>
		<li>additionalDriver - Additional driver fee.</li>
		<li>airportAccessFee - Airport Access fee.</li>
		<li>dropOffCharge - Drop off charge.</li>
		<li>vehicleLicensingFee - Vehicle licensing fee.</li>
		<li>touristTax - Tourist tax.</li>
		<li>prepaidGasoline - Prepaid gasoline charge.</li>
		<li>navigationSystem - Navigation system.</li>
		<li>childSeat - Child seat charge.</li>
		<li>luggageRack - Luggage rack charge.</li>
		<li>collisionDamageInsurance - Collision damage insurance.</li>
		<li>liabilityInsurance - Liability insurance.</li>
		<li>mobilePhone - Mobile phone base charge.</li>
		<li>other - Other charges.</li>
		</ul>
-->
<!ATTLIST CarRentalFee
    type (baseRate | additionalDriver | airportAccessFee | dropOffCharge |
          vehicleLicensingFee | touristTax | prepaidGasoline |
          navigationSystem | childSeat | luggageRack | collisionDamageInsurance |
          liabilityInsurance | mobilePhone | other) "baseRate"
>

<!-- Hotel -->

<!--
    Hotel detail information.
	
    Vendor: Hotel vendor information.

    Address: Physical address of the hotel. This is might be different
        from the address specified in the Vendor field as the vendor
        address might be the head quarter address.

    RoomType: The type of room reserved.

    BookingClassCode: Hotel booking class code.

    Meal: Any complementary meals that are included with the room. For
        example, complementary continental breakfast.

    Rate: Hotel rate information. Multiple rates can be specified. For
        example, the night rate, valet parking rate, and other rates.

    AvailablePrice: Other available prices that user did not pick. Available
        prices can be from the same vendor or different vendor.
-->
<!ELEMENT HotelDetail (Vendor,
                       Address,
                       RoomType,
                       BookingClassCode?,
                       Meal*,
                       Rate*,
                       AvailablePrice*)>
<!--
	@attr travelSegment Textual information to identify this travel segment. This information is specific 
		to the Travel Booking Provider.

	@attr arrivalTime Date and time of arrival at the hotel. This is used as an advisory to the hotel vendor 
		for the arrival time.

	@attr departureTime Date and time of departure from the hotel. This is used as an advisory to the hotel 
		vendor for the departure time.

	@attr checkinTime The official checkin time. For example, most hotel checkin time is 3:00 PM.

	@attr checkoutTime The official checkout time. For example, most hotel checkout time is 12:00 PM (noon).

	@attr earlyCheckinAllowed Set to "yes" to indicate that early checkout is allowed.

	@attr lateCheckoutAllowed Set to "yes" to indicate that late checkout is allowed.
-->
<!ATTLIST HotelDetail
    travelSegment   %string;         #REQUIRED
    arrivalTime     %datetime.tz;    #REQUIRED
    departureTime   %datetime.tz;    #REQUIRED
    checkinTime     %time.tz;        #REQUIRED
    checkoutTime    %time.tz;        #REQUIRED
    earlyCheckinAllowed (yes)        #IMPLIED
    lateCheckoutAllowed (yes)        #IMPLIED
>

<!--
    Information about a hotel room.
	
    Description: Textual description of the hotel room.

    Amenities: List of amenities for this hotel room.
-->
<!ELEMENT RoomType (Description?, Amenities*)>
<!--
	@attr smoking Set to "yes" or "no" to indicate whether it is a smoking a non-smoking room.

	@attr numberOfBed The number of beds in this room.

	@attr bedType The bed type in this room.
-->
<!ATTLIST RoomType
    smoking        (yes | no)   #REQUIRED
    numberOfBed    %r8;         #IMPLIED
    bedType        (king | queen | full | double | single | other)    #IMPLIED
>

<!--
    Textual description of the amenities for the hotel room.
	
    Description: Text description of this amenities. For example,
        DSL connection, two telephone lines, and other information
        about a hotel room.
-->
<!ELEMENT Amenities (Description)>

<!-- Rail -->

<!--
    Rail detail information. A rail detail can have multiple legs.
	
    TripType: Trip type for this rail.

    RailLeg: The different rail legs that make up this rail detail.

    AvailablePrice: Other available prices that the user did not pick for for this
        rail trip.

    Penalty: Penalty (if any) associated with this rail trip.
-->
<!ELEMENT RailDetail (TripType,
                      RailLeg+,
                      AvailablePrice*,
                      Penalty?)>

<!--
    Rail leg is used to express information regarding a single leg for
    a rail travel.
	
    Vendor: Rail vendor information.

    RailLegOrigin: Rail originating location.

    RailLegDestination: Rail Destination location.

    BookingClassCode: Rail booking class code.

    Rate: Rate information (if any) for this rail leg. If specified,
        all the rates in all rail legs must add up to the total at the
        travel line item level.

    Meal: Meal served for this leg (if any).
-->
<!ELEMENT RailLeg (Vendor,
                   RailLegOrigin,
                   RailLegDestination,
                   BookingClassCode?,
                   Rate?,
                   Meal*)>
<!--
	@attr travelSegment Textual information to identify this travel segment. This information is specific 
		to the Travel Booking Provider.

	@attr departureTime Date and time of departure at the originating location.

	@attr arrivalTime Date and time of arrival at the destination location.

	@attr trainNumber Train number for this rail leg.

	@attr seatNumber Seat number.

	@attr carType Type of the rail car.
-->
<!ATTLIST RailLeg
    travelSegment   %string;        #REQUIRED
    departureTime   %datetime.tz;   #REQUIRED
    arrivalTime     %datetime.tz;   #REQUIRED
    trainNumber     %string;        #REQUIRED
    seatNumber      %string;        #IMPLIED
    carType        (sleeper | seat) #IMPLIED
>

<!--
    The origin of a particular rail leg. This can be an airport or a
    physical address of a rail station.
	
    Airport: An airport location.

    Address: A physical address of a rail station.
-->
<!ELEMENT RailLegOrigin (Airport | Address)>

<!--
    The destination of a particular rail leg. This can be an airport
    or a physical address of a rail station.
	
    Airport: An airport location.

    Address: A physical address of a rail station.
-->
<!ELEMENT RailLegDestination (Airport | Address)>

<!-- Travel/Expense common definitions -->

<!--
    The type of the trip. This is used by Air and Rail to indicate
    round trip, one way or multi-leg trip.
-->
<!ELEMENT TripType EMPTY>
<!-- 
	@attr type Type of trip (enumeration). Possible values:
		<ul>
		<li>round - A round trip.</li>
		<li>oneWay - A one way trip.</li>
		<li>multiLeg - A multi leg or open jaw trip.</li>
		</ul>
-->
<!ATTLIST TripType
    type    (round | oneWay | multiLeg) #REQUIRED
>

<!--
    Information about a vendor that is providing this service.
	
    Address: The physical address of the vendor. This address normally is
        the business or head quarter address of the vendor.

    SupplierID: Supplier ID for this vendor. This is just a (domain, value)
        pair so that Travel Booking Providers have the flexibility to
        define their id's according to an arbitrary convention (for example,
        (DUNS, 12345), (TaxID, 88888888)).

        Note: Multiple SupplierID elements can be specified. This is so
        that Travel Booking Provider can have a single implementation
        that works with different Buyer implementations that use
        a different SupplierID domain.
-->

<!ELEMENT Vendor (Address, SupplierID*)>
<!--
	@attr preferred Set to "yes" or "no" to indicate whether this vendor is a preferred vendor.
-->
<!ATTLIST Vendor
    preferred (yes | no) #REQUIRED
>

<!--
    Textual terms and conditions associated with an air fare, car
    rental, hotel, or rail. For example, a car rental terms and
    conditions normally include boundary limit, additional mileage
    charges, gasoline charge and other restriction information.

    Multiple terms and conditions can be attached to a single travel
		line item.
	
	Description: Textual terms and conditions.
-->
<!ELEMENT TermsAndConditions (Description+)>

<!--
    Policy violation (if any) that results from the user picking this
    particular travel. Policy violations are associated with an
    individual line item of travel and not associated at the header
    level. This allows for clear identification of the violation with
    the individual line item.
	
    Description: Textual description of this violation.

    PolicyViolationJustification: Justification for this violation given by user 
		normally picked from a standard list of justifications at the Travel Booking
		Provider web site.

    Comments: Additional comments to further clarify the justification given
        by user.
-->
<!ELEMENT PolicyViolation (Description,
                           PolicyViolationJustification,
                           Comments?)>
<!--
	@attr level Violation level. Possible values:
		<ul>
		<li>warning - a non serious violation.</li>
		<li>violation - a serious violation of company policy.</li>
		</ul>
-->
<!ATTLIST PolicyViolation
	level (warning | violation) #REQUIRED
>

<!--
    Justification given by the user why they violate the company
    travel policy. This justification value is normally picked from a
    pick list at the Travel Booking Provider web site.
	
    Description: The justification of the violation picked from a standard list
        at the Travel Booking Provider web site.
-->
<!ELEMENT PolicyViolationJustification (Description)>

<!--
    Penalty (if any) for this travel segment.
	
    Money: The penalty amount.

    Description: Textual description of the cause of the penalty. For example,
        change fee associated with air ticket.
-->
<!ELEMENT Penalty (Money, Description)>

<!--
    Other avaible price. AvailablePrice is used to express what other
    prices are available that the traveler did not pick.
    AvailablePrice is can be used to capture the lowest price, the
    highest price, the lowest compliant price, and the hight compliant
    price.
	
    Money: The amount of the other available price.

    Description: Textual description of the available price. It explain
        how this price was derived at. For example, a non direct flight
        exists with the flight number can be put here.
-->
<!ELEMENT AvailablePrice (Money, Description?)>
<!--
	@attr type Type of available price (enumeration). Possible values:
		<ul>
		<li>lowest - The lowest price possible regardless of the traveling policies.</li>
		<li>lowestCompliant - The lowest price that still complies with the travel policies.</li>
		<li>highestCompliant - The highest price that still complies with the travel policies.</li>
		<li>highest - The highest price possible regardless of the travel policies.</li>
		<li>other - Other, specify in the description.</li>
		</ul>
-->
<!ATTLIST AvailablePrice
    type (lowest | lowestCompliant | highestCompliant | highest | other) #REQUIRED
>

<!--
    Booking class code used by airfare, car rental, hotel, and rail to
    indicate the class.
	
    Description: Textual description of the code.
-->
<!ELEMENT BookingClassCode (Description?)>
<!--
	@attr domain The domain for this code, for example, IATA.

	@attr code An industry standard code, or per agreement of buyer-travel booking provider pair.
-->
<!ATTLIST BookingClassCode
    domain  %string;    #REQUIRED
    code    %string;    #REQUIRED
>

<!--
    Define a physical location and its address. This can be used to 
    describe places such as airport, bus station, ferry dock, railway
    station and so on.
	
    Address: Indicates the address of the place.
-->
<!ELEMENT Place (Address?)>
<!--
	@attr domain Qualifier or domain for the code. For example, airport code is IATA standard, the domain 
		value would be "IATA"; however bus station code may be local specific.
		
	@attr code Indicates the code the may represent the place. For example, airport can have an airport code.
-->
<!ATTLIST Place
    code           %string;     #IMPLIED
    domain         %string;     #IMPLIED
>

<!--
    Defines places such as city, airport or railway where trip or leg originated.
-->
<!ELEMENT LegOrigin (Place)>

<!--
    Defines places such as city, airport or railway code where trip or leg ended.
-->
<!ELEMENT LegDestination (Place)>
