const fs = require('fs');
const path = require('path');

const contentDbPath = path.join(__dirname, '../StayHub_ContentDb_Data.sql');
const contentSql = fs.readFileSync(contentDbPath, 'utf8');

// Parse Categories
const categories = [];
const catRegex = /\((\d+),\s*N'([^']+)',/g;
let catMatch;
while ((catMatch = catRegex.exec(contentSql)) !== null) {
    if (categories.length < 12) {
        categories.push({ id: parseInt(catMatch[1]), name: catMatch[2] });
    }
}

// Parse TourismInformation
const tourismInfo = [];
const lines = contentSql.split('\n');
for (let line of lines) {
    if (line.trim().startsWith('(') && line.includes('N\'Vietnam\'')) {
        const tourRegex = /\((\d+),\s*N'([^']+)',\s*'([^']+)',\s*N'((?:[^']|'')*)',\s*N'([^']+)',\s*N'([^']+)',\s*N'Vietnam',\s*([\d\.-]+),\s*([\d\.-]+),/;
        const m = tourRegex.exec(line);
        if (m) {
            tourismInfo.push({
                id: parseInt(m[1]),
                name: m[2],
                type: m[3],
                desc: m[4].replace(/''/g, "'"),
                address: m[5],
                city: m[6],
                lat: parseFloat(m[7]),
                lng: parseFloat(m[8])
            });
        }
    }
}

const cities34 = [
    "Hanoi", "Ho Chi Minh City", "Hai Phong", "Da Nang", "Can Tho", "Hue",
    "Quang Ninh", "Ninh Binh", "Lao Cai", "Ha Giang", "Lam Dong", "Kien Giang", 
    "Ba Ria - Vung Tau", "Binh Thuan", "Thanh Hoa", "Nghe An", "Quang Binh", 
    "Phu Yen", "Binh Dinh", "Gia Lai", "Dak Lak", "Tay Ninh", "Dong Nai", 
    "Ben Tre", "Vinh Long", "Dong Thap", "An Giang", "Ca Mau", "Bac Ninh", 
    "Vinh Phuc", "Hai Duong", "Quang Nam", "Khanh Hoa", "Son La"
];

const fallbackCoords = {
    "Hanoi": { lat: 21.0285, lng: 105.8542 },
    "Ho Chi Minh City": { lat: 10.8231, lng: 106.6297 },
    "Hai Phong": { lat: 20.8449, lng: 106.6881 },
    "Da Nang": { lat: 16.0544, lng: 108.2022 },
    "Can Tho": { lat: 10.0452, lng: 105.7469 },
    "Hue": { lat: 16.4637, lng: 107.5909 },
    "Quang Ninh": { lat: 21.0069, lng: 107.2925 },
    "Ninh Binh": { lat: 20.2539, lng: 105.9753 },
    "Lao Cai": { lat: 22.4856, lng: 103.9707 },
    "Ha Giang": { lat: 22.8233, lng: 104.9836 },
    "Lam Dong": { lat: 11.5645, lng: 108.0069 },
    "Kien Giang": { lat: 10.0159, lng: 105.0809 },
    "Ba Ria - Vung Tau": { lat: 10.4962, lng: 107.1685 },
    "Binh Thuan": { lat: 11.0883, lng: 108.0673 },
    "Thanh Hoa": { lat: 19.8057, lng: 105.7766 },
    "Nghe An": { lat: 19.3444, lng: 104.8872 },
    "Quang Binh": { lat: 17.4833, lng: 106.5997 },
    "Phu Yen": { lat: 13.0881, lng: 109.3204 },
    "Binh Dinh": { lat: 13.7828, lng: 109.2197 },
    "Gia Lai": { lat: 13.9833, lng: 108.0 },
    "Dak Lak": { lat: 12.8333, lng: 108.0 },
    "Tay Ninh": { lat: 11.3, lng: 106.1 },
    "Dong Nai": { lat: 11.0, lng: 107.1667 },
    "Ben Tre": { lat: 10.2333, lng: 106.3833 },
    "Vinh Long": { lat: 10.25, lng: 105.9667 },
    "Dong Thap": { lat: 10.5, lng: 105.6333 },
    "An Giang": { lat: 10.518, lng: 105.1167 },
    "Ca Mau": { lat: 9.1769, lng: 105.15 },
    "Bac Ninh": { lat: 21.1861, lng: 106.0763 },
    "Vinh Phuc": { lat: 21.3114, lng: 105.5975 },
    "Hai Duong": { lat: 20.9381, lng: 106.3153 },
    "Quang Nam": { lat: 15.584, lng: 108.0264 },
    "Khanh Hoa": { lat: 12.25, lng: 109.1833 },
    "Son La": { lat: 21.328, lng: 103.9145 }
};

const getRandom = (arr) => arr[Math.floor(Math.random() * arr.length)];
const getRandomInt = (min, max) => Math.floor(Math.random() * (max - min + 1)) + min;
const formatSqlString = (str) => str ? `N'${str.replace(/'/g, "''")}'` : 'NULL';

const heritagePrefixes = ["Discover", "The Magic of", "Wonders of", "Deep Dive into", "Unforgettable", "Journey to"];
const localPrefixes = ["Highlights of", "Essential", "Ultimate", "Best of", "Fascinating", "Authentic", "Hidden Gems of"];
const thematicAdjectives = ["Luxury", "Wellness", "Adventure", "Culinary", "Trekking", "Romantic", "Eco-friendly", "Photography"];

const reviewsGood = [
    "Absolutely amazing experience! The guide was very knowledgeable.",
    "A must-do when you visit. Completely exceeded our expectations.",
    "Breathtaking views and great food. Highly recommended.",
    "Very well organized from start to finish. We felt very safe and comfortable.",
    "Incredible heritage and history. I learned so much.",
    "Our tour guide made this trip truly special. 5 stars!",
    "Such a beautiful destination! Will definitely come back."
];

const reviewsMixed = [
    "The itinerary was a bit rushed on day 2, but overall fantastic.",
    "Good tour, but the weather wasn't on our side.",
    "Average tour, some places were too crowded.",
    "It was okay, but the food could be better.",
    "Not bad, but I think the ticket price is a bit too high for what you get.",
    "Enjoyed the morning activities, but the afternoon was quite boring."
];

const reviewsBad = [
    "Terrible experience, would not recommend this to anyone.",
    "The guide was very rude and unhelpful.",
    "Waste of money and time. The places were dirty and crowded.",
    "Completely disorganized. We waited for the bus for 2 hours.",
    "Nothing like the pictures. Very disappointing trip.",
    "Poor service and bad food. I want a refund."
];

let tours = [];
let itineraries = [];
let schedules = [];
let scheduleItineraries = [];
let scheduleTickets = [];
let scheduleStaffs = [];
let reviews = [];
let reviewReplies = [];
let promotions = [];
let promotionTickets = [];
let wishlists = [];

let tourIdCounter = 1;
let itineraryIdCounter = 1;
let scheduleIdCounter = 1;
let scheduleItineraryIdCounter = 1;
let ticketIdCounter = 1;
let staffIdCounter = 1;
let reviewIdCounter = 1;
let replyIdCounter = 1;
let promoIdCounter = 1;
let wishlistIdCounter = 1;

// Generate 50 Promotions
const promoTypes = ['Percent', 'Fixed'];
for (let p = 1; p <= 50; p++) {
    const type = getRandom(promoTypes);
    const val = type === 'Percent' ? getRandomInt(5, 30) : getRandomInt(5, 20) * 10000;
    promotions.push({
        id: promoIdCounter++,
        code: `PROMO${p}X2026`,
        name: `Special Offer ${p}`,
        desc: `Exclusive ${type === 'Percent' ? val + '%' : val + ' VND'} discount on selected tours.`,
        type: type,
        val: val,
        max: type === 'Percent' ? getRandomInt(2, 5) * 100000 : 'NULL',
        start: `DATEADD(DAY, -${getRandomInt(10, 30)}, GETDATE())`,
        end: `DATEADD(DAY, ${getRandomInt(30, 90)}, GETDATE())`
    });
}

function getLocalArea(address, city) {
    if (!address) return city;
    const parts = address.split(',').map(p => p.trim());
    if (parts.length > 2) {
        return parts[parts.length - 2]; 
    } else if (parts.length === 2) {
        return parts[0];
    }
    return city;
}

// Generating Tours
for (let city of cities34) {
    const citySites = tourismInfo.filter(t => t.city.includes(city) || (city === 'Hue' && t.city.includes('Thua Thien Hue')));
    
    for (let i = 1; i <= 20; i++) {
        const cat = getRandom(categories) || { id: 1, name: 'General' };
        
        let targetSite = null;
        if (citySites.length > 0) {
            targetSite = getRandom(citySites);
        }

        let tourName = "";
        let desc = "";
        let address = targetSite ? targetSite.address : `Central Area, ${city}, Vietnam`;
        let localArea = getLocalArea(address, city);

        if (i <= 8 && targetSite) {
            tourName = `${getRandom(heritagePrefixes)} ${targetSite.name}`;
            desc = `Embark on an unforgettable journey to ${targetSite.name}. This meticulously crafted tour focuses on deep cultural immersion and exploring famous heritage sites.`;
        } else if (i <= 14) {
            tourName = `${getRandom(localPrefixes)} ${localArea}`;
            desc = `Experience the vibrant life and rich history of ${localArea}. This comprehensive tour takes you through bustling markets, historic landmarks, and scenic countryside.`;
        } else {
            const theme = getRandom(thematicAdjectives);
            tourName = `${theme} Retreat in ${localArea}`;
            desc = `Indulge in a specialized ${theme.toLowerCase()} experience in ${localArea}. Perfect for travelers seeking unique activities tailored to their interests.`;
        }

        const tour = {
            id: tourIdCounter++,
            categoryId: cat.id,
            name: tourName,
            desc: desc,
            country: 'Vietnam',
            city: city,
            address: address,
            imageUrl: `https://picsum.photos/seed/tour_img_${tourIdCounter}/800/600`,
            createdBy: getRandomInt(1, 10),
            createdAt: `DATEADD(DAY, -${getRandomInt(700, 1000)}, GETDATE())` // 2023-2024
        };
        tours.push(tour);

        // Generate Wishlists
        const wlCount = getRandomInt(3, 5);
        for(let w = 0; w < wlCount; w++) {
            wishlists.push({
                id: wishlistIdCounter++,
                customerId: getRandomInt(26, 558),
                tourId: tour.id
            });
        }

        const sentimentRoll = Math.random();
        let sentimentProfile = 'good';
        if (sentimentRoll > 0.85) sentimentProfile = 'bad';
        else if (sentimentRoll > 0.6) sentimentProfile = 'mixed';

        const revCount = getRandomInt(5, 10);
        for(let r = 0; r < revCount; r++) {
            let rating = 5;
            let comment = "";

            if (sentimentProfile === 'good') {
                rating = Math.random() > 0.2 ? getRandomInt(4, 5) : 3;
                comment = rating >= 4 ? getRandom(reviewsGood) : getRandom(reviewsMixed);
            } else if (sentimentProfile === 'mixed') {
                rating = getRandomInt(2, 4);
                comment = rating <= 2 ? getRandom(reviewsBad) : (rating === 3 ? getRandom(reviewsMixed) : getRandom(reviewsGood));
            } else { // bad
                rating = Math.random() > 0.2 ? getRandomInt(1, 2) : 3;
                comment = rating <= 2 ? getRandom(reviewsBad) : getRandom(reviewsMixed);
            }

            const reviewId = reviewIdCounter++;
            reviews.push({
                id: reviewId,
                tourId: tour.id,
                customerId: getRandomInt(26, 558),
                rating: rating,
                comment: comment,
                createdAt: `DATEADD(DAY, -${getRandomInt(1, 800)}, GETDATE())` // 2024-2026
            });
            
            const replyChance = (sentimentProfile === 'bad' || sentimentProfile === 'mixed') ? 0.6 : 0.2;
            if (Math.random() < replyChance) {
                let replyComment = rating <= 3 
                    ? "We sincerely apologize for your experience. Our team is looking into this to ensure it doesn't happen again." 
                    : "Thank you so much for your wonderful feedback! We hope to see you again soon.";
                
                reviewReplies.push({
                    id: replyIdCounter++,
                    reviewId: reviewId,
                    userId: getRandomInt(2, 8), // Manager user IDs
                    comment: replyComment,
                    createdAt: `DATEADD(DAY, -${getRandomInt(1, 800)}, GETDATE())` // 2024-2026
                });
            }
        }

        const usedSites = new Set();
        const genericSpots = [
            `${localArea} Walking Street`,
            `Traditional Restaurant in ${localArea}`,
            `${localArea} Central Square`,
            `Local Cafe near ${localArea}`,
            `${city} Cultural Center`,
            `${localArea} Shopping District`,
            `Historic site near ${localArea}`
        ];

        const totalDays = getRandomInt(2, 3);
        for (let d = 1; d <= totalDays; d++) {
            const acts = [
                { title: 'Morning Exploration', start: '08:00:00', end: '11:30:00' },
                { title: 'Afternoon Discovery', start: '13:30:00', end: '17:00:00' },
                { title: 'Evening Experience', start: '18:30:00', end: '21:00:00' }
            ];

            for (let act of acts) {
                let locName = '';
                let lat = fallbackCoords[city].lat + (Math.random() - 0.5) * 0.1;
                let lng = fallbackCoords[city].lng + (Math.random() - 0.5) * 0.1;
                let tourismInfoId = 'NULL';

                let availableSites = citySites.filter(s => !usedSites.has(s.id));
                
                // Prioritize unused official sites, else use generic spots
                if (availableSites.length > 0 && Math.random() > 0.3) {
                    let site = getRandom(availableSites);
                    usedSites.add(site.id);
                    locName = site.name;
                    lat = site.lat;
                    lng = site.lng;
                    tourismInfoId = site.id;
                } else {
                    if (act.title.includes('Evening')) {
                        locName = `${city} Night Market`;
                    } else {
                        locName = getRandom(genericSpots);
                    }
                }

                itineraries.push({
                    id: itineraryIdCounter++,
                    tourId: tour.id,
                    dayNumber: d,
                    title: `Day ${d}: ${act.title}`,
                    desc: `Enjoy the ${act.title.toLowerCase()} visiting famous local attractions such as ${locName}.`,
                    start: act.start,
                    end: act.end,
                    locName: locName,
                    lat: lat,
                    lng: lng,
                    tourismInfoId: tourismInfoId
                });
            }
        }

        for (let sc = 0; sc < 10; sc++) {
            const scheduleId = scheduleIdCounter++;
            const daysOffset = getRandomInt(-800, 500); // 2024 to mid 2027
            
            schedules.push({
                id: scheduleId,
                tourId: tour.id,
                departure: `DATEADD(DAY, ${daysOffset}, GETDATE())`,
                returnDate: `DATEADD(DAY, ${daysOffset + totalDays}, GETDATE())`,
                note: `Regular scheduled tour`
            });

            const tourItins = itineraries.filter(it => it.tourId === tour.id);
            for (let ti of tourItins) {
                scheduleItineraries.push({
                    id: scheduleItineraryIdCounter++,
                    scheduleId: scheduleId,
                    dayNumber: ti.dayNumber,
                    date: `DATEADD(DAY, ${daysOffset + ti.dayNumber - 1}, GETDATE())`,
                    title: ti.title,
                    desc: ti.desc,
                    start: ti.start,
                    end: ti.end,
                    locName: ti.locName,
                    lat: ti.lat,
                    lng: ti.lng,
                    tourismInfoId: ti.tourismInfoId
                });
            }

            scheduleStaffs.push({
                id: staffIdCounter++,
                scheduleId: scheduleId,
                staffId: getRandomInt(11, 20),
                role: 'Tour Guide'
            });

            const typesForSchedule = [1];
            // Randomly pick 1 or 2 more types from [2, 4, 5] (Child, Senior, Student - no Infant)
            let extraTypes = [2, 4, 5].sort(() => 0.5 - Math.random()).slice(0, getRandomInt(1, 2));
            typesForSchedule.push(...extraTypes);

            for (let typeId of typesForSchedule) {
                let price = 0;
                let qty = 10;
                if (typeId === 1) { price = getRandomInt(80, 300) * 10000; qty = 30; } // Adult
                else if (typeId === 2) { price = getRandomInt(50, 150) * 10000; qty = 10; } // Child
                else if (typeId === 3) { price = getRandomInt(10, 30) * 10000; qty = 5; } // Infant
                else if (typeId === 4) { price = getRandomInt(60, 200) * 10000; qty = 5; } // Senior
                else if (typeId === 5) { price = getRandomInt(60, 200) * 10000; qty = 15; } // Student
                
                const tId = ticketIdCounter++;
                scheduleTickets.push({
                    id: tId,
                    scheduleId: scheduleId,
                    ticketTypeId: typeId,
                    price: price,
                    qty: qty,
                    sold: getRandomInt(0, Math.floor(qty / 2))
                });
                
                if (Math.random() > 0.5) {
                    const promo = getRandom(promotions);
                    promotionTickets.push({ promoId: promo.id, ticketId: tId });
                }
            }
        }
    }
}

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

function generateInsertSqlNoIdentity(tableName, columns, rows) {
    if (rows.length === 0) return '';
    let sql = '';
    const batchSize = 1000;
    for (let i = 0; i < rows.length; i += batchSize) {
        const batch = rows.slice(i, i + batchSize);
        sql += `INSERT INTO ${tableName} (${columns.join(', ')}) VALUES\n`;
        sql += batch.map(row => `(${row.join(', ')})`).join(',\n') + ';\nGO\n';
    }
    return sql;
}

let finalSql = `
/* =========================================================
   STAYHUB CATALOG DATABASE DATA SEED
   Auto-generated by generate_catalog_data.js
   Covers: ${tours.length} Diverse Tours (Heritage, District, Thematic) across 34 specific administrative units in Vietnam.
   Includes: Diverse Sentimental Reviews (${reviews.length}), ReviewReplies (${reviewReplies.length}), Promotions (${promotions.length}).
   Itineraries (Multi-activity per day), Schedules, Tickets, Staff.
   ========================================================= */

USE StayHub_CatalogDb;
GO

DELETE FROM ReviewReplies;
DELETE FROM Reviews;
DELETE FROM Wishlists;
DELETE FROM PromotionTickets;
DELETE FROM Promotions;
DELETE FROM TourScheduleTickets;
DELETE FROM TourScheduleStaffs;
DELETE FROM TourScheduleItineraries;
DELETE FROM TourSchedules;
DELETE FROM TourItineraries;
DELETE FROM Tours;

DBCC CHECKIDENT ('ReviewReplies', RESEED, 0);
DBCC CHECKIDENT ('Reviews', RESEED, 0);
DBCC CHECKIDENT ('Wishlists', RESEED, 0);
DBCC CHECKIDENT ('Promotions', RESEED, 0);
DBCC CHECKIDENT ('TourScheduleTickets', RESEED, 0);
DBCC CHECKIDENT ('TourScheduleStaffs', RESEED, 0);
DBCC CHECKIDENT ('TourScheduleItineraries', RESEED, 0);
DBCC CHECKIDENT ('TourSchedules', RESEED, 0);
DBCC CHECKIDENT ('TourItineraries', RESEED, 0);
DBCC CHECKIDENT ('Tours', RESEED, 0);
GO\n\n`;

const tourRows = tours.map(t => [
    t.id, t.categoryId, formatSqlString(t.name), formatSqlString(t.desc),
    formatSqlString(t.country), formatSqlString(t.city), formatSqlString(t.address),
    formatSqlString(t.imageUrl), formatSqlString("Vietnam National Administration of Tourism"), 'NULL',
    t.createdBy, 'NULL', t.createdAt, 'NULL', "'Active'"
]);
finalSql += generateInsertSql('Tours', 
    ['Id', 'CategoryId', 'Name', 'Description', 'Country', 'City', 'Address', 'ImageUrl', 'SourceName', 'SourceUrl', 'CreatedBy', 'UpdatedBy', 'CreatedAt', 'UpdatedAt', 'Status'],
    tourRows);

const itinRows = itineraries.map(t => [
    t.id, t.tourId, t.dayNumber, formatSqlString(t.title), formatSqlString(t.desc),
    `'${t.start}'`, `'${t.end}'`, formatSqlString(t.locName), t.lat, t.lng, t.tourismInfoId
]);
finalSql += generateInsertSql('TourItineraries',
    ['Id', 'TourId', 'DayNumber', 'Title', 'Description', 'StartDuration', 'EndDuration', 'LocationName', 'LocationLat', 'LocationLng', 'TourismInfoId'],
    itinRows);

const schedRows = schedules.map(s => [
    s.id, s.tourId, s.departure, s.returnDate, formatSqlString(s.note)
]);
finalSql += generateInsertSql('TourSchedules',
    ['Id', 'TourId', 'DepartureDate', 'ReturnDate', 'Note'],
    schedRows);

const schedItinRows = scheduleItineraries.map(t => [
    t.id, t.scheduleId, t.dayNumber, t.date, formatSqlString(t.title), formatSqlString(t.desc),
    `'${t.start}'`, `'${t.end}'`, formatSqlString(t.locName), t.lat, t.lng, t.tourismInfoId
]);
finalSql += generateInsertSql('TourScheduleItineraries',
    ['Id', 'ScheduleId', 'DayNumber', 'ItineraryDate', 'Title', 'Description', 'StartDuration', 'EndDuration', 'LocationName', 'LocationLat', 'LocationLng', 'TourismInfoId'],
    schedItinRows);

const staffRows = scheduleStaffs.map(s => [
    s.id, s.scheduleId, s.staffId, formatSqlString(s.role)
]);
finalSql += generateInsertSql('TourScheduleStaffs',
    ['Id', 'ScheduleId', 'StaffId', 'AssignedRole'],
    staffRows);

const ticketRows = scheduleTickets.map(t => [
    t.id, t.scheduleId, t.ticketTypeId, t.price, t.qty, t.sold, t.qty - t.sold, 1, 'NULL'
]);
finalSql += generateInsertSql('TourScheduleTickets',
    ['Id', 'ScheduleId', 'TicketTypeId', 'Price', 'Quantity', 'SoldQuantity', 'AvailableQuantity', 'IsActive', 'Note'],
    ticketRows);

const promoRows = promotions.map(p => [
    p.id, formatSqlString(p.code), formatSqlString(p.name), formatSqlString(p.desc),
    formatSqlString(p.type), p.val, p.max, p.start, p.end, "'Active'", 'GETDATE()'
]);
finalSql += generateInsertSql('Promotions',
    ['Id', 'Code', 'Name', 'Description', 'DiscountType', 'DiscountValue', 'MaxDiscountAmount', 'StartDate', 'EndDate', 'Status', 'CreatedAt'],
    promoRows);

const promoTicketRows = promotionTickets.map(pt => [pt.promoId, pt.ticketId]);
finalSql += generateInsertSqlNoIdentity('PromotionTickets', ['PromotionId', 'TourScheduleTicketId'], promoTicketRows);

const wishlistRows = wishlists.map(w => [w.id, w.customerId, w.tourId]);
finalSql += generateInsertSql('Wishlists', ['Id', 'CustomerId', 'TourId'], wishlistRows);

const reviewRows = reviews.map(r => [
    r.id, r.customerId, r.tourId, r.rating, formatSqlString(r.comment), r.createdAt, 'NULL', 0
]);
finalSql += generateInsertSql('Reviews',
    ['Id', 'CustomerId', 'TourId', 'Rating', 'Comment', 'CreatedAt', 'UpdatedAt', 'IsHidden'],
    reviewRows);

const replyRows = reviewReplies.map(r => [
    r.id, r.reviewId, r.userId, formatSqlString(r.comment), r.createdAt, 'NULL'
]);
finalSql += generateInsertSql('ReviewReplies',
    ['Id', 'ReviewId', 'UserId', 'Content', 'CreatedAt', 'UpdatedAt'],
    replyRows);

fs.writeFileSync(path.join(__dirname, '../StayHub_CatalogDb_Data.sql'), finalSql, 'utf8');
console.log(`Successfully generated StayHub_CatalogDb_Data.sql with ${tours.length} district-aware tours, ${reviews.length} diverse sentiment reviews, and ${promotions.length} promotions.`);
