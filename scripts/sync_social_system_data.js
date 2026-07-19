const fs = require('fs');
const path = require('path');
const readline = require('readline');

const catalogDbPath = path.join(__dirname, '../StayHub_CatalogDb_Data.sql');
const socialDbPath = path.join(__dirname, '../StayHub_SocialDb_Data.sql');
const systemDbPath = path.join(__dirname, '../StayHub_SystemDb_Data.sql');

// 1. Parse Schedules from CatalogDb
const catalogSql = fs.readFileSync(catalogDbPath, 'utf8');
const schedules = [];
const linesCat = catalogSql.split('\n');
let inSchedules = false;
for (let line of linesCat) {
    if (line.includes('INSERT INTO TourSchedules')) inSchedules = true;
    else if (line.includes('SET IDENTITY_INSERT TourSchedules OFF')) inSchedules = false;
    
    if (inSchedules && line.startsWith('(')) {
        const parts = line.split(',');
        if (parts.length >= 2) {
            const id = parseInt(parts[0].replace('(', '').trim());
            if (!isNaN(id)) schedules.push(id);
        }
    }
}

if (schedules.length === 0) {
    console.error("No schedules found in CatalogDb!");
    process.exit(1);
}

const getRandomSchedule = () => schedules[Math.floor(Math.random() * schedules.length)];

async function processSqlFile(filePath, tablesConfig) {
    console.log(`Processing ${filePath}...`);
    const tempPath = filePath + '.tmp';
    
    const readStream = fs.createReadStream(filePath, { encoding: 'utf8' });
    const writeStream = fs.createWriteStream(tempPath, { encoding: 'utf8' });
    
    const rl = readline.createInterface({
        input: readStream,
        crlfDelay: Infinity
    });

    let currentTable = null;
    let config = null;

    let modifiedCount = 0;

    for await (const line of rl) {
        let newLine = line;
        
        // Detect table blocks
        if (line.includes('INSERT INTO ')) {
            currentTable = null;
            config = null;
            for (const table of Object.keys(tablesConfig)) {
                if (line.includes(`INSERT INTO ${table}`)) {
                    currentTable = table;
                    config = tablesConfig[table];
                    break;
                }
            }
        } else if (line.includes('SET IDENTITY_INSERT') && line.includes(' OFF')) {
            currentTable = null;
            config = null;
        } else if (line.trim() === 'GO' || line.trim() === '') {
            // Might be out of an insert block if not identity_insert block
        }

        // If inside a target table block and line looks like a data tuple
        if (currentTable && config && line.trim().startsWith('(')) {
            // A tuple might span multiple lines if it has string data, but let's assume standard formatting for numeric/short fields
            // Or we can just use a simple regex replacing if the tuple is on a single line.
            // Using a simple split approach for comma-separated values outside quotes is hard, but since ScheduleId is numeric,
            // we can regex match the start of the tuple.

            // regex to match: (val1, val2, val3, ...
            // We need to replace the Nth value.
            // Let's do it by matching commas, ignoring commas inside quotes if possible.
            // However, ScheduleId is usually one of the first few columns (id, user, scheduleId).
            
            // Simpler robust way for specific tables:
            if (currentTable === 'TourMoments') {
                // (Id, UserId, ScheduleId, Caption, ...
                // e.g. (1, 5, 234, N'Hello...
                // Regex: /^\((\d+),\s*(\d+),\s*(\d+)/
                newLine = line.replace(/^\((\d+),\s*(\d+),\s*(\d+)/, (match, p1, p2, p3) => {
                    modifiedCount++;
                    return `(${p1}, ${p2}, ${getRandomSchedule()}`;
                });
            } else if (currentTable === 'ChatRooms') {
                // (Id, Name, Type, ScheduleId, ...
                // e.g. (1, NULL, 'Tour', 234, ... or (2, 'VIP', 'Direct', NULL, ...
                // Regex: /^\((\d+),\s*([^,]+),\s*([^,]+),\s*([\d]+|NULL)/
                newLine = line.replace(/^\((\d+),\s*([^,]+),\s*([^,]+),\s*([\d]+|NULL)/, (match, p1, p2, p3, p4) => {
                    // Only replace if it wasn't NULL originally, or if we want to sync Tour chats
                    if (p4 === 'NULL') {
                        return match;
                    }
                    modifiedCount++;
                    return `(${p1}, ${p2}, ${p3}, ${getRandomSchedule()}`;
                });
            } else if (currentTable === 'LocationLogs') {
                // (Id, UserId, ScheduleId, Lat, Lng, ...
                // Regex: /^\((\d+),\s*(\d+),\s*(\d+)/
                newLine = line.replace(/^\((\d+),\s*(\d+),\s*(\d+)/, (match, p1, p2, p3) => {
                    modifiedCount++;
                    return `(${p1}, ${p2}, ${getRandomSchedule()}`;
                });
            }
        }
        
        writeStream.write(newLine + '\n');
    }

    writeStream.end();
    
    await new Promise(resolve => writeStream.on('finish', resolve));
    
    fs.unlinkSync(filePath);
    fs.renameSync(tempPath, filePath);
    
    console.log(`Finished ${filePath}. Modified ${modifiedCount} rows.`);
}

async function run() {
    await processSqlFile(socialDbPath, {
        'TourMoments': true,
        'ChatRooms': true
    });
    
    if (fs.existsSync(systemDbPath)) {
        await processSqlFile(systemDbPath, {
            'LocationLogs': true
        });
    }
}

run().catch(err => console.error(err));
