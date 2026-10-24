// API models mirroring the backend DTOs (through the gateway).

export interface Paged<T> {
    items: T[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNext: boolean;
    hasPrevious: boolean;
}

// ---------- Orders ----------
export interface AddressDto {
    street: string;
    city: string;
    postalCode: string;
    country: string;
    lat?: number | null;
    lng?: number | null;
}
export interface CreateOrderItem {
    description: string;
    quantity: number;
    weightKg: number;
}
export interface CreateOrder {
    customerId: string;
    pickup: AddressDto;
    delivery: AddressDto;
    items: CreateOrderItem[];
}
export interface OrderSummary {
    id: string;
    orderNumber: string;
    customerId: string;
    status: string;
    createdOnUtc: string;
}
export interface OrderItem {
    description: string;
    quantity: number;
    weightKg: number;
}
export interface OrderDetail {
    id: string;
    orderNumber: string;
    customerId: string;
    status: string;
    pickup: AddressDto;
    delivery: AddressDto;
    createdOnUtc: string;
    confirmedOnUtc?: string | null;
    cancelledOnUtc?: string | null;
    items: OrderItem[];
}

// ---------- Billing ----------
export interface InvoiceLineInput {
    description: string;
    quantity: number;
    unitPrice: number;
}
export interface CreateInvoice {
    customerId: string;
    currency: string;
    lines: InvoiceLineInput[];
}
export interface InvoiceSummary {
    id: string;
    customerId: string;
    status: string;
    total: number;
    currency: string;
    createdOnUtc: string;
}
export interface InvoiceLine {
    description: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
}
export interface InvoiceDetail {
    id: string;
    customerId: string;
    currency: string;
    status: string;
    total: number;
    issuedOnUtc?: string | null;
    dueDateUtc?: string | null;
    paidOnUtc?: string | null;
    createdOnUtc: string;
    lines: InvoiceLine[];
}

// ---------- Tracking ----------
export interface ShipmentSummary {
    id: string;
    loadId: string;
    carrierId: string;
    status: string;
    currentLatitude?: number | null;
    currentLongitude?: number | null;
    lastPositionAtUtc?: string | null;
    createdOnUtc: string;
    originCity?: string;
    destinationCity?: string;
    originLatitude?: number;
    originLongitude?: number;
    destinationLatitude?: number;
    destinationLongitude?: number;
}
export interface ShipmentLocationMessage {
    shipmentId: string;
    latitude: number;
    longitude: number;
    speedKph?: number | null;
    status: string;
    atUtc: string;
}

// ---------- Planning ----------
export interface LoadSummary {
    id: string;
    reference: string;
    status: string;
    originCity: string;
    destinationCity: string;
    totalWeightKg: number;
    orderCount: number;
    carrierId?: string | null;
    createdOnUtc: string;
}
export interface LoadOrderLine {
    orderId: string;
    description: string;
    weightKg: number;
}
export interface LoadDetail {
    id: string;
    reference: string;
    status: string;
    originCity: string;
    destinationCity: string;
    totalWeightKg: number;
    carrierId?: string | null;
    routeId?: string | null;
    createdOnUtc: string;
    plannedOnUtc?: string | null;
    confirmedOnUtc?: string | null;
    cancelledOnUtc?: string | null;
    orders: LoadOrderLine[];
}
export interface RouteStopLine {
    sequence: number;
    kind: string;
    city: string;
    address: string;
    latitude: number;
    longitude: number;
}
export interface RouteDetail {
    id: string;
    loadId: string;
    status: string;
    createdOnUtc: string;
    finalizedOnUtc?: string | null;
    stops: RouteStopLine[];
}

// ---------- Documents ----------
export interface DocumentSummary {
    id: string;
    ownerId: string;
    category: string;
    storageRef: string;
    status: string;
    createdOnUtc: string;
}
export interface CreateDocument {
    ownerId: string;
    category: string;
    storageRef: string;
}

// ---------- Notifications ----------
export interface NotificationSummary {
    id: string;
    recipient: string;
    channel: string;
    templateId: string;
    status: string;
    createdOnUtc: string;
}
export interface CreateNotification {
    recipient: string;
    channel: string;
    templateId: string;
}

// ---------- Analytics ----------
export interface ReportDefinitionSummary {
    id: string;
    name: string;
    query: string;
    status: string;
    createdOnUtc: string;
}
export interface CreateReportDefinition {
    name: string;
    query: string;
}

// ---------- Compliance ----------
export interface ComplianceCheckSummary {
    id: string;
    subjectId: string;
    subjectType: string;
    ruleCode: string;
    jurisdiction: string;
    status: string;
    createdOnUtc: string;
    checkedOnUtc?: string | null;
    findingCount: number;
    hasBlockingFindings: boolean;
}
export interface ComplianceFindingLine {
    code: string;
    severity: string;
    message: string;
    recordedOnUtc: string;
}
export interface ComplianceCheckDetail {
    id: string;
    subjectId: string;
    subjectType: string;
    ruleCode: string;
    jurisdiction: string;
    status: string;
    createdOnUtc: string;
    checkedOnUtc?: string | null;
    waivedReason?: string | null;
    findings: ComplianceFindingLine[];
}
export interface CreateComplianceCheck {
    subjectId: string;
    subjectType: string;
    ruleCode: string;
    jurisdiction: string;
}

// ---------- Integration ----------
export interface IntegrationConnectorSummary {
    id: string;
    systemName: string;
    endpoint: string;
    status: string;
    createdOnUtc: string;
    lastSyncOnUtc?: string | null;
    consecutiveFailures: number;
    disabledReason?: string | null;
}
export interface CreateIntegrationConnector {
    systemName: string;
    endpoint: string;
}

// ---------- Intelligence ----------
export interface PredictionRequestSummary {
    id: string;
    modelId: string;
    predictionType: string;
    status: string;
    createdOnUtc: string;
    finishedOnUtc?: string | null;
    confidence?: number | null;
    error?: string | null;
}
export interface CreatePredictionRequest {
    modelId: string;
    predictionType: string;
}

// ---------- Carriers ----------
export interface CarrierSummary {
    id: string;
    legalName: string;
    country: string;
    status: string;
    totalCapacityKg: number;
    fleetSize: number;
    driverCount: number;
    createdOnUtc: string;
}
export interface VehicleLine {
    id: string;
    plate: string;
    type: string;
    capacityKg: number;
    status: string;
}
export interface DriverLine {
    id: string;
    fullName: string;
    licenceNumber: string;
    status: string;
}
export interface CarrierDetail {
    id: string;
    legalName: string;
    registrationNumber: string;
    country: string;
    status: string;
    totalCapacityKg: number;
    fleetSize: number;
    driverCount: number;
    createdOnUtc: string;
    verifiedOnUtc?: string | null;
    suspendedOnUtc?: string | null;
    vehicles: VehicleLine[];
    drivers: DriverLine[];
}
