const fs = require('fs');
const path = require('path');
const { v4: uuidv4 } = require('uuid');

const catalogDbPath = path.join(__dirname, '../StayHub_CatalogDb_Data.sql');
const catalogSql = fs.readFileSync(catalogDbPath, 'utf8');

const bookingDbPath = path.join(__dirname, '../StayHub_BookingDb_Data.sql');
const paymentDbPath = path.join(__dirname, '../StayHub_PaymentDb_Data.sql');

// 1. Parse Schedules from CatalogDb
const schedules = [];
const linesCat = catalogSql.split('\n');
let inSchedules = false;
for (let line of linesCat) {
    if (line.includes('INSERT INTO TourSchedules')) inSchedules = true;
    else if (line.includes('SET IDENTITY_INSERT TourSchedules OFF')) inSchedules = false;
    
    if (inSchedules && line.startsWith('(')) {
        const m = line.match(/^\((\d+),\s*\d+,\s*DATEADD\(DAY,\s*(-?\d+),/);
        if (m) {
            schedules.push({
                id: parseInt(m[1]),
                offset: parseInt(m[2])
            });
        }
    }
}

// 2. Parse ScheduleTickets
// (Id, ScheduleId, TicketTypeId, Price, Quantity, SoldQuantity, AvailableQuantity, IsActive, Note)
const scheduleTickets = [];
const lines = catalogSql.split('\n');
let inTickets = false;
for (let line of lines) {
    if (line.includes('INSERT INTO TourScheduleTickets')) inTickets = true;
    else if (line.includes('SET IDENTITY_INSERT TourScheduleTickets OFF')) inTickets = false;
    
    if (inTickets && line.startsWith('(')) {
        const parts = line.split(',');
        if (parts.length >= 4) {
            scheduleTickets.push({
                id: parseInt(parts[0].replace('(', '').trim()),
                scheduleId: parseInt(parts[1].trim()),
                ticketTypeId: parseInt(parts[2].trim()), // 1 = Adult, 2 = Child
                price: parseInt(parts[3].trim())
            });
        }
    }
}

const getRandom = (arr) => arr[Math.floor(Math.random() * arr.length)];
const getRandomInt = (min, max) => Math.floor(Math.random() * (max - min + 1)) + min;
const formatSqlString = (str) => str ? `N'${str.replace(/'/g, "''")}'` : 'NULL';

const vouchers = [null, null, null, 'EARLYBIRD25', 'HONEYMOON25', 'STUDENT2025', 'SUMMER25', 'TET2025'];
const statuses = ['Paid', 'Paid', 'Paid', 'Paid', 'Cancelled', 'Request to Cancel'];
const notes = [null, null, 'We have elderly participants - kindly arrange accessible transport.', 'Please confirm pickup location.', 'We have a child under 5 years old.', 'Please arrange vegetarian meals.'];

let orders = [];
let orderDetails = [];
let tickets = [];
let cancellationRequests = [];
let transactions = [];

let orderIdCounter = 1;
let orderDetailIdCounter = 1;
let ticketIdCounter = 1;
let cancelReqIdCounter = 1;
let txIdCounter = 1;

// Generating Orders for each customer
for (let custId = 26; custId <= 558; custId++) {
    const numOrders = getRandomInt(3, 10);
    
    for (let o = 0; o < numOrders; o++) {
        const schedule = getRandom(schedules);
        const scheduleId = schedule.id;
        const availableTickets = scheduleTickets.filter(t => t.scheduleId === scheduleId);
        
        if (availableTickets.length === 0) continue; 

        let totalQty = 0;
        let totalAmount = 0;
        let orderedTickets = [];

        for (let t of availableTickets) {
            if (t.ticketTypeId === 1) { // Adult always
                let qty = getRandomInt(1, 4);
                orderedTickets.push({ typeId: 1, scheduleTicketId: t.id, qty: qty, price: t.price, total: qty * t.price });
                totalQty += qty;
                totalAmount += qty * t.price;
            } else {
                if (Math.random() > 0.5) { // 50% chance for other types
                    let qty = getRandomInt(1, 2);
                    orderedTickets.push({ typeId: t.ticketTypeId, scheduleTicketId: t.id, qty: qty, price: t.price, total: qty * t.price });
                    totalQty += qty;
                    totalAmount += qty * t.price;
                }
            }
        }

        if (orderedTickets.length === 0 || !orderedTickets.find(t => t.typeId === 1)) continue;

        const voucher = getRandom(vouchers);
        let discount = 0;
        if (voucher) {
            discount = totalAmount * 0.15; // Mock 15% discount
        }
        const finalAmount = totalAmount - discount;

        const status = getRandom(statuses);
        const orderId = orderIdCounter++;

        const orderDaysOffset = schedule.offset - getRandomInt(1, 60);

        orders.push({
            id: orderId,
            customerId: custId,
            scheduleId: scheduleId,
            totalQty: totalQty,
            discount: discount,
            voucher: voucher,
            totalAmount: totalAmount,
            finalAmount: finalAmount,
            note: getRandom(notes),
            status: status,
            orderedAt: `DATEADD(DAY, ${orderDaysOffset}, GETDATE())`,
            token: uuidv4()
        });

        let ticketCheckInStatus = "'Pending'";
        if (status === 'Cancelled' || status === 'Request to Cancel') {
            ticketCheckInStatus = "'Cancelled'";
        } else if (status === 'Paid' && schedule.offset < 0) {
            ticketCheckInStatus = "'CheckedIn'";
        }

        for (let ot of orderedTickets) {
            const odId = orderDetailIdCounter++;
            orderDetails.push({
                id: odId,
                orderId: orderId,
                ticketTypeId: ot.typeId,
                scheduleTicketId: ot.scheduleTicketId,
                qty: ot.qty,
                unitPrice: ot.price,
                totalPrice: ot.total
            });

            for (let t = 0; t < ot.qty; t++) {
                let dob = '';
                let namePrefix = '';
                if (ot.typeId === 1) { dob = `'199${getRandomInt(0, 9)}-0${getRandomInt(1, 9)}-1${getRandomInt(0, 9)}'`; namePrefix = 'Adult'; }
                else if (ot.typeId === 2) { dob = `'201${getRandomInt(5, 9)}-0${getRandomInt(1, 9)}-1${getRandomInt(0, 9)}'`; namePrefix = 'Child'; }
                else if (ot.typeId === 3) { dob = `'202${getRandomInt(2, 5)}-0${getRandomInt(1, 9)}-1${getRandomInt(0, 9)}'`; namePrefix = 'Infant'; }
                else if (ot.typeId === 4) { dob = `'195${getRandomInt(0, 9)}-0${getRandomInt(1, 9)}-1${getRandomInt(0, 9)}'`; namePrefix = 'Senior'; }
                else if (ot.typeId === 5) { dob = `'200${getRandomInt(0, 5)}-0${getRandomInt(1, 9)}-1${getRandomInt(0, 9)}'`; namePrefix = 'Student'; }

                tickets.push({
                    id: ticketIdCounter++,
                    orderDetailId: odId,
                    userId: (ot.typeId === 1 || ot.typeId === 5) && Math.random() > 0.5 ? custId : 'NULL',
                    ticketTypeId: ot.typeId,
                    name: `${namePrefix} Traveler ${t+1}`,
                    idCard: `079${ot.typeId}000${getRandomInt(10000, 99999)}`,
                    dob: dob,
                    gender: Math.random() > 0.5 ? "'Male'" : "'Female'",
                    nationality: "N'Vietnam'",
                    qrCode: uuidv4(),
                    checkIn: ticketCheckInStatus
                });
            }
        }

    // Cancellations
    if (status === 'Cancelled' || status === 'Request to Cancel') {
        const cancelOffset = orderDaysOffset + getRandomInt(1, 14);
        cancellationRequests.push({
            id: cancelReqIdCounter++,
            orderId: orderId,
            customerId: orders[orders.length - 1].customerId,
            bankName: "N'Vietcombank'",
            accNo: `'101${getRandomInt(1000000, 9999999)}'`,
            accName: "N'NGUYEN VAN A'",
            reqAt: `DATEADD(DAY, ${cancelOffset}, GETDATE())`,
            original: finalAmount,
            fee: finalAmount * 0.2,
            feePercent: 20,
            refund: finalAmount * 0.8,
            reason: getRandom([
                "N'Unexpected emergency'", 
                "N'Flight delayed'", 
                "N'Health issues'"
            ]),
            status: status === 'Cancelled' ? "'Approved'" : "'Pending'",
            rejectReason: 'NULL',
            processedAt: status === 'Cancelled' ? `DATEADD(DAY, ${cancelOffset + 1}, GETDATE())` : 'NULL',
            processedBy: status === 'Cancelled' ? getRandomInt(2, 8) : 'NULL'
        });
    }

    // Transactions
    if (status !== 'Pending') {
        const txStatus = status === 'Cancelled' ? 'Success' : (Math.random() > 0.05 ? 'Success' : 'Failed');
        transactions.push({
            id: txIdCounter++,
            orderId: orderId,
            amount: finalAmount,
            provider: getRandom(["'VNPay'", "'MoMo'"]),
            txId: `'TXN${getRandomInt(10000000, 99999999)}'`,
            status: `'${txStatus}'`
        });
    }
    } // End of inner order loop
} // End of outer customer loop

// Generate Bookings SQL
function generateInsertSql(tableName, columns, rows) {
    if (rows.length === 0) return '';
    let sql = `SET IDENTITY_INSERT ${tableName} ON;\nGO\n`;
    const batchSize = 1000;
    for (let i = 0; i < rows.length; i += batchSize) {
        const batch = rows.slice(i, i + batchSize);
        sql += `INSERT INTO ${tableName} (${columns.join(', ')}) VALUES\n`;
        sql += batch.map(row => `(${row.join(', ')})`).join(',\n') + ';\nGO\n';
    }
    sql += `SET IDENTITY_INSERT ${tableName} OFF;\nGO\n\n`;
    return sql;
}

let bookingSql = `/* =========================================================
   STAYHUB BOOKING DATABASE DATA SEED
   Auto-generated by generate_booking_payment_data.js
   Covers: Orders, OrderDetails, Tickets, CancellationRequests
   ========================================================= */

USE StayHub_BookingDb;
GO

DELETE FROM CancellationRequests;
DELETE FROM Tickets;
DELETE FROM OrderDetails;
DELETE FROM Orders;

DBCC CHECKIDENT ('CancellationRequests', RESEED, 0);
DBCC CHECKIDENT ('Tickets', RESEED, 0);
DBCC CHECKIDENT ('OrderDetails', RESEED, 0);
DBCC CHECKIDENT ('Orders', RESEED, 0);
GO\n\n`;

const orderRows = orders.map(o => [
    o.id, o.customerId, o.scheduleId, o.totalQty, o.discount, 
    o.voucher ? `'${o.voucher}'` : 'NULL', o.totalAmount, o.finalAmount, 
    formatSqlString(o.note), `'${o.status}'`, o.orderedAt, `'${o.token}'`
]);
bookingSql += generateInsertSql('Orders', ['Id', 'CustomerId', 'ScheduleId', 'TotalQuantity', 'DiscountValue', 'VoucherCode', 'TotalAmount', 'FinalAmount', 'Note', 'Status', 'OrderedAt', 'InviteToken'], orderRows);

const odRows = orderDetails.map(od => [
    od.id, od.orderId, od.ticketTypeId, od.scheduleTicketId, od.qty, od.unitPrice, od.totalPrice
]);
bookingSql += generateInsertSql('OrderDetails', ['Id', 'OrderId', 'TicketTypeId', 'TourScheduleTicketId', 'Quantity', 'UnitPrice', 'TotalPrice'], odRows);

const tkRows = tickets.map(t => [
    t.id, t.orderDetailId, t.userId, t.ticketTypeId, formatSqlString(t.name), `'${t.idCard}'`, 
    t.dob, t.gender, t.nationality, `'${t.qrCode}'`, t.checkIn
]);
bookingSql += generateInsertSql('Tickets', ['Id', 'OrderDetailId', 'UserId', 'TicketTypeId', 'AttendeeName', 'IdCard', 'DateOfBirth', 'Gender', 'Nationality', 'QrCode', 'CheckInStatus'], tkRows);

const crRows = cancellationRequests.map(c => [
    c.id, c.orderId, c.customerId, c.bankName, c.accNo, c.accName, c.reqAt, c.original, c.fee, c.feePercent, c.refund, c.reason, c.status, c.rejectReason, c.processedAt, c.processedBy
]);
bookingSql += generateInsertSql('CancellationRequests', ['Id', 'OrderId', 'CustomerId', 'BankName', 'AccountNumber', 'AccountHolderName', 'RequestedAt', 'OriginalAmount', 'CancellationFee', 'FeePercent', 'RefundAmount', 'Reason', 'Status', 'RejectReason', 'ProcessedAt', 'ProcessedBy'], crRows);

fs.writeFileSync(bookingDbPath, bookingSql, 'utf8');

// Generate Payments SQL
let paymentSql = `/* =========================================================
   STAYHUB PAYMENT DATABASE DATA SEED
   Auto-generated by generate_booking_payment_data.js
   Covers: Transactions
   ========================================================= */

USE StayHub_PaymentDb;
GO

DELETE FROM Transactions;
DBCC CHECKIDENT ('Transactions', RESEED, 0);
GO\n\n`;

const txRows = transactions.map(t => [
    t.id, t.orderId, t.amount, t.provider, t.txId, t.status
]);
paymentSql += generateInsertSql('Transactions', ['Id', 'OrderId', 'Amount', 'Provider', 'ProviderTxnId', 'Status'], txRows);

fs.writeFileSync(paymentDbPath, paymentSql, 'utf8');

console.log(`Successfully generated BookingDb with ${orders.length} orders and PaymentDb with ${transactions.length} transactions.`);
