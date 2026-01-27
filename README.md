# Skolmat-projektet (MENO) 🍽️🏫
Digitalisera skolmatenkäter (allergier/kostpreferenser) och minska matsvinn genom planering, uppföljning och tydlig data.

## Innehåll
- [Översikt](#översikt)
- [Funktioner](#funktioner)
- [Tech stack](#tech-stack)
- [Repo-struktur](#repo-struktur)
- [Krav](#krav)
- [Kom igång lokalt](#kom-igång-lokalt)
  - [Backend](#backend)
  - [Frontend](#frontend)
- [Miljövariabler](#miljövariabler)
- [Databas & migreringar](#databas--migreringar)
- [Auth & Roller](#auth--roller)
- [API-dokumentation](#api-dokumentation)
- [Kodstandard & Git-flöde](#kodstandard--git-flöde)
- [Vanliga fel](#vanliga-fel)
- [Roadmap](#roadmap)
- [Licens](#licens)

---

## Översikt
Projektet hjälper skolor att:
- samla in allergier och kostpreferenser digitalt,
- skapa och kommunicera veckomenyer,
- koppla allergener till rätter,
- skapa individuella meal plans för elever,
- och mäta/rapportera matsvinn över tid.

---

## Funktioner

### Användare (Student/User)
- Logga in
- Se veckans meny
- Se sin profil + allergier/kostpreferenser
- Se sin meal plan

### Admin
- Se rapporter/statistik för matsvinn i dashboarden
- Se sin profil

---

## Tech stack

### Backend
- **.NET 8 / C#**
- **Clean Architecture**
- ASP.NET Core Web API
- Entity Framework Core
- Identity (JWT)
- Swagger/OpenAPI

### Frontend
- **React** (t.ex. Vite)
- React Router
- Axios/Fetch
- (Valfritt) UI-lib: MUI / Tailwind / Chakra

### Database
- SQL Server (LocalDB / SQLExpress i dev)

---

## Repo-struktur

Exempel (rekommenderat):

