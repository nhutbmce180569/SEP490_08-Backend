const fs = require('fs');
const path = require('path');

const contentDbPath = path.join(__dirname, '../StayHub_ContentDb_Data.sql');
const contentSql = fs.readFileSync(contentDbPath, 'utf8');

const tourismInfo = [];
const lines = contentSql.split('\n');
for(let line of lines) {
    if (line.trim().startsWith('(') && line.includes('N\'Vietnam\'')) {
        const tourRegex = /\((\d+),\s*N'([^']+)',\s*'([^']+)',\s*N'((?:[^']|'')*)',\s*N'([^']+)',\s*N'([^']+)',\s*N'Vietnam',\s*([\d\.-]+),\s*([\d\.-]+),/;
        const m = tourRegex.exec(line);
        if (m) {
            tourismInfo.push({
                id: parseInt(m[1]),
                name: m[2],
                city: m[6],
                lat: parseFloat(m[7]),
                lng: parseFloat(m[8])
            });
        }
    }
}

console.log(`Parsed ${tourismInfo.length} tourism sites.`);

const uniqueCities = [...new Set(tourismInfo.map(t => t.city))];
console.log(`Unique cities (${uniqueCities.length}):`, uniqueCities);

const my34Cities = [
    "Hanoi", "Ho Chi Minh City", "Hai Phong", "Da Nang", "Can Tho", "Hue",
    "Quang Ninh", "Ninh Binh", "Lao Cai", "Ha Giang", "Lam Dong", "Kien Giang", 
    "Ba Ria - Vung Tau", "Binh Thuan", "Thanh Hoa", "Nghe An", "Quang Binh", 
    "Phu Yen", "Binh Dinh", "Gia Lai", "Dak Lak", "Tay Ninh", "Dong Nai", 
    "Ben Tre", "Vinh Long", "Dong Thap", "An Giang", "Ca Mau", "Bac Ninh", 
    "Vinh Phuc", "Hai Duong", "Quang Nam", "Khanh Hoa", "Son La"
];

let missing = [];
for (let c of my34Cities) {
    if (!uniqueCities.includes(c) && !uniqueCities.includes(c.replace(' City', '')) && !uniqueCities.some(u => u.includes(c) || c.includes(u))) {
        missing.push(c);
    }
}
console.log('Missing cities:', missing);
