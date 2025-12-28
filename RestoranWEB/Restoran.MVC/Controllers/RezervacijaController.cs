using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Restoran.BLL.DTOs;
using Restoran.BLL.Services;
using Restoran.DAL.Entities;
using Restoran.DAL.UnitOfWork;

namespace Restoran.MVC.Controllers
{
    // Kuvar ne sme uopšte da ulazi u kontroler
    [Authorize(Roles = "Menadzer,Konobar,Gost")]
    public class RezervacijaController : Controller
    {
        private readonly IRezervacijaService _service;
        private readonly IUnitOfWork _uow;

        public RezervacijaController(IRezervacijaService service, IUnitOfWork uow)
        {
            _service = service;
            _uow = uow;
        }

        // ----------- LISTA ----------
        public async Task<IActionResult> Index()
        {
            IEnumerable<RezervacijaDto> model;

            // Menadžer, Konobar vide sve
            if (User.IsInRole("Menadzer") || User.IsInRole("Konobar"))
                model = await _service.GetAllAsync();
            else
                // Gost vidi SAMO svoje
                model = (await _service.GetAllAsync())
                        .Where(r => r.CreatedBy == User.Identity!.Name)
                        .ToList();

            return View(model);
        }

        // ----------- CREATE ----------
        public async Task<IActionResult> Create()
        {
            await PopulateStoloviDropDown(DateTime.Today);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RezervacijaDto dto)
        {
            dto.CreatedBy = User.Identity!.Name;
            if (!ModelState.IsValid)
            {
                await PopulateStoloviDropDown(dto.Datum);
                return View(dto);
            }

            try
            {
                int rezId = await _service.CreateAsync(dto);

                // ako gost izabrao narudžbinu – kreiraj je i poveži
                if (!string.IsNullOrEmpty(dto.NarudzbinaProizvod))
                {
                    var nar = new Narudzbina
                    {
                        BrojStola = dto.BrojStola,
                        Proizvod = dto.NarudzbinaProizvod,
                        CreatedBy = User.Identity.Name!,
                        Datum = DateTime.Now
                    };
                    await _uow.Narudzbine.AddAsync(nar);
                    await _uow.SaveChangesAsync();          // sada nar ima Id

                    // poveži sa rezervacijom
                    var rez = await _uow.Rezervacije.GetByIdAsync(rezId);
                    rez.NarudzbinaId = nar.Id;
                    await _uow.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                await PopulateStoloviDropDown(dto.Datum);
                return View(dto);
            }
        }

        // ----------- EDIT ----------
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null) return NotFound();

            // Gost sme SAMO svoje
            if (User.IsInRole("Gost") && dto.CreatedBy != User.Identity!.Name)
                return RedirectToAction("AccessDenied", "Account");

            await PopulateStoloviDropDown(dto.Datum);
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RezervacijaDto dto)
        {
            if (!ModelState.IsValid)
            {
                await PopulateStoloviDropDown(dto.Datum);
                return View(dto);
            }
            try
            {
                // 1. osnovne izmene rezervacije
                await _service.UpdateAsync(dto);

                var rez = await _uow.Rezervacije.GetByIdAsync(dto.IDRezervacije);

                bool imaNovu = !string.IsNullOrEmpty(dto.NarudzbinaProizvod);

                // 2. rukovanje narudžbinom
                if (rez.NarudzbinaId.HasValue)        // ranije je imala narudžbinu
                {
                    var stara = await _uow.Narudzbine.GetByIdAsync(rez.NarudzbinaId.Value);
                    if (stara != null)
                    {
                        if (!imaNovu)                 // korisnik ostavio prazno -> obriši
                        {
                            await _uow.Narudzbine.DeleteAsync(stara);
                            rez.NarudzbinaId = null;
                        }
                        else                          // samo promeni proizvod
                        {
                            stara.Proizvod = dto.NarudzbinaProizvod;
                        }
                    }
                }
                else                                   // ranije nije imala
                {
                    if (imaNovu)                        // kreiraj novu
                    {
                        var nova = new Narudzbina
                        {
                            BrojStola = dto.BrojStola,
                            Proizvod = dto.NarudzbinaProizvod,
                            CreatedBy = User.Identity.Name!,
                            Datum = DateTime.Now
                        };
                        await _uow.Narudzbine.AddAsync(nova);
                        await _uow.SaveChangesAsync();  // da dobijemo Id
                        rez.NarudzbinaId = nova.Id;
                    }
                }

                await _uow.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                await PopulateStoloviDropDown(dto.Datum);
                return View(dto);
            }
        }

        // ----------- DELETE (samo Menadžer) ----------
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null) return NotFound();
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null) return NotFound();

            // Gost sme samo svoje
            if (User.IsInRole("Gost") && dto.CreatedBy != User.Identity!.Name)
                return RedirectToAction("AccessDenied", "Account");

            await _service.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // ----------- JSON za zauzete stolove -----------
        [HttpGet]
        public async Task<JsonResult> GetZauzetiStoloviJson(DateTime datum)
        {
            var rezervacije = await _uow.Rezervacije.GetAllAsync();
            var zauzeti = rezervacije
                          .Where(r => r.Datum.Date == datum.Date)
                          .Select(r => r.Sto.BrojStola)
                          .Distinct()
                          .ToList();
            return Json(zauzeti);
        }

        // ----------- Pomoćna: popuni padajući meni ----------
        private async Task PopulateStoloviDropDown(DateTime datum)
        {
            var stolovi = await _uow.Stolovi.GetAllAsync();
            var zauzeti = await GetZauzetiStoloviAsync(datum);
            ViewBag.ZauzetiStolovi = zauzeti;
            ViewBag.Stolovi = new SelectList(stolovi, "BrojStola", "BrojStola");
        }

        private async Task<List<int>> GetZauzetiStoloviAsync(DateTime datum)
        {
            var rezervacije = await _uow.Rezervacije.GetAllAsync();
            return rezervacije
                   .Where(r => r.Datum.Date == datum.Date)
                   .Select(r => r.Sto.BrojStola)
                   .Distinct()
                   .ToList();
        }
    }
}