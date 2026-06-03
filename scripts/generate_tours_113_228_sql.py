#!/usr/bin/env python3
"""Generate SQL seed for Tours 113-228 with VNAT/UNESCO provenance."""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from apply_tour_provenance import build_row_113_228, sql_escape
from tour_provenance_config import CITIES_113_228

DURATIONS = ["1D0N", "2D1N", "3D2N", "4D3N", "5D4N"]
THEMES = [
    "Signature Experience",
    "Local Discovery",
    "Scenic Escape",
    "Culture and Nature",
    "Weekend Getaway",
]
CATEGORIES = [1, 2, 3, 4, 5, 6, 8, 9, 11, 12]


def main() -> None:
    out = Path(__file__).resolve().parent.parent / "StayHub_Tours_113_228_Insert.sql"
    lines: list[str] = [
        "/* Auto-generated: Tours 113-228 — real places with VNAT/UNESCO sources */",
        "USE StayHub_CatalogDb;",
        "GO",
        "",
        "SET IDENTITY_INSERT Tours ON;",
        "INSERT INTO Tours (Id, CategoryId, CreatedBy, Name, Description, Country, City, Address, ImageUrl, SourceName, SourceUrl, Status) VALUES",
    ]

    tour_rows = []
    schedule_id = 5000
    ticket_id = 8000
    itinerary_id = 5000
    tour_schedules = [
        "SET IDENTITY_INSERT TourSchedules ON;",
        "INSERT INTO TourSchedules (Id, TourId, DepartureDate, ReturnDate, Status) VALUES",
    ]
    tour_tickets = [
        "SET IDENTITY_INSERT TourScheduleTickets ON;",
        "INSERT INTO TourScheduleTickets (Id, ScheduleId, TicketTypeId, Price, AvailableQuantity, IsActive) VALUES",
    ]
    tour_itins = [
        "SET IDENTITY_INSERT TourItineraries ON;",
        "INSERT INTO TourItineraries (Id, TourId, DayNumber, Title, Description, StartDuration, EndDuration, LocationName, LocationLat, LocationLng, TourismInfoId) VALUES",
    ]

    for i, (city, province, highlight) in enumerate(CITIES_113_228, start=113):
        if i > 228:
            break
        row = build_row_113_228(i)
        if not row:
            continue
        tour_rows.append(row.rstrip(","))

        tour_schedules.append(
            f"({schedule_id}, {i}, DATEADD(DAY, {(i % 45) + 7}, CAST(GETDATE() AS date)), "
            f"DATEADD(DAY, {(i % 45) + 9}, CAST(GETDATE() AS date)), 'Open'),"
        )
        base_price = 1_800_000 + (i % 12) * 350_000
        tour_tickets.append(f"({ticket_id}, {schedule_id}, 1, {base_price}, 25, 1),")
        tour_itins.append(
            f"({itinerary_id}, {i}, 1, N'Explore {sql_escape(city)}', "
            f"N'Day tour aligned with {sql_escape(province)} tourism guides.', "
            f"'08:00', '17:00', N'{sql_escape(city)}', "
            f"{10.0 + (i % 80) * 0.01:.3f}, {105.0 + (i % 80) * 0.01:.3f}, NULL),"
        )
        schedule_id += 1
        ticket_id += 1
        itinerary_id += 1

    if tour_rows:
        tour_rows[-1] = tour_rows[-1] + ";"
    else:
        tour_rows.append(";")

    lines.append(",\n".join(tour_rows))
    lines += ["SET IDENTITY_INSERT Tours OFF;", "GO", ""]

    for block in (tour_schedules, tour_tickets, tour_itins):
        if len(block) > 2:
            block[-1] = block[-1].rstrip(",") + ";"
        lines += block + ["SET IDENTITY_INSERT TourSchedules OFF;", "GO", ""]

    out.write_text("\n".join(lines), encoding="utf-8")
    print(f"Wrote {out} ({len(CITIES_113_228)} tours)")


if __name__ == "__main__":
    main()
