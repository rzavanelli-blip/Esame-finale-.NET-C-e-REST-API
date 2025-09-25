using Esame_C__e_Reset_Api.Modelli;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuro la policy CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();

// Middleware per controllo accesso con "Access" header
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/api/login")
    {
        await next.Invoke();
        return;
    }

    if (context.Request.Headers.TryGetValue("Access", out var valoreToken))
    {
        if (valoreToken == "AB12345")
        {
            await next.Invoke();
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Accesso negato: token non valido.");
        }
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("Accesso negato: token non presente.");
    }
});

// Endpoint di LOGIN
app.MapPost("/api/login", (Credenziali cred) =>
{
    if (string.IsNullOrWhiteSpace(cred.Password) || string.IsNullOrWhiteSpace(cred.Username))
        return Results.BadRequest("Username e/o password mancanti.");

    if (cred.Username == "Roberta" && cred.Password == "Roberta")
        return Results.Ok(new { token = "AB12345" });

    return Results.BadRequest("Credenziali non valide.");
})
.WithName("Login")
.WithOpenApi(op => new(op)
{
    Summary = "Effettua il login",
    Description = "Login con username e password. Se corrette, restituisce un token da usare negli header (chiave 'Access') per accedere agli altri endpoint."
})
.Accepts<Credenziali>("application/json")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

// Dati di esempio (Database in-memory)
var studenti = new List<Studente>
{
    new Studente
    {
        Id = 1,
        Nome = "Mario",
        Cognome = "Rossi",
        CorsiIscritti = new List<Corso>()
    },
    new Studente
    {
        Id = 2,
        Nome = "Luisa",
        Cognome = "Verdi",
        CorsiIscritti = new List<Corso>()
    }
};

var corsi = new List<Corso>
{
    new Corso { Id = 101, NomeCorso = "Programmazione C#", Descrizione = "Introduzione a .NET" },
    new Corso { Id = 102, NomeCorso = "Web Development", Descrizione = "Sviluppo di applicazioni web" },
    new Corso { Id = 103, NomeCorso = "Database SQL", Descrizione = "Gestione di database relazionali" }
};


var iscrizioni = new List<Iscrizione>
{
    new Iscrizione { StudenteId = 1, CorsoId = 101, DataIscrizione = "2025-09-25", Stato = "Attivo" },
    new Iscrizione { StudenteId = 2, CorsoId = 102, DataIscrizione = "2025-09-26", Stato = "Completato" }
};


// Iscriviamo gli studenti ad alcuni corsi
studenti.First(s => s.Id == 1).CorsiIscritti.Add(corsi.First(c => c.Id == 101));
studenti.First(s => s.Id == 1).CorsiIscritti.Add(corsi.First(c => c.Id == 102));
studenti.First(s => s.Id == 2).CorsiIscritti.Add(corsi.First(c => c.Id == 101));


// ================== ENDPOINT STUDENTI ==================

app.MapGet("/api/studenti", () => Results.Ok(studenti))
   .WithName("GetStudenti")
   .WithOpenApi(op => new(op)
   {
       Summary = "Ottieni tutti gli studenti",
       Description = "Restituisce la lista completa degli studenti con i corsi a cui sono iscritti."
   })
   .Produces<List<Studente>>(StatusCodes.Status200OK);

app.MapGet("/api/studenti/{id}", (int id) =>
{
    var studente = studenti.FirstOrDefault(s => s.Id == id);
    return studente is null ? Results.NotFound() : Results.Ok(studente);
})
.WithName("GetStudenteById")
.WithOpenApi(op => new(op)
{
    Summary = "Ottieni un singolo studente",
    Description = "Restituisce i dettagli di uno studente specifico in base all'ID."
})
.Produces<Studente>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/studenti", (Studente nuovoStudente) =>
{
    nuovoStudente.Id = studenti.Count > 0 ? studenti.Max(s => s.Id) + 1 : 1;
    studenti.Add(nuovoStudente);
    return Results.Created($"/api/studenti/{nuovoStudente.Id}", nuovoStudente);
})
.WithName("CreateStudente")
.WithOpenApi(op => new(op)
{
    Summary = "Crea un nuovo studente",
    Description = "Aggiunge un nuovo studente alla lista."
})
.Accepts<Studente>("application/json")
.Produces<Studente>(StatusCodes.Status201Created);

app.MapPut("/api/studenti/{id}", (int id, Studente studenteAggiornato) =>
{
    var studenteEsistente = studenti.FirstOrDefault(s => s.Id == id);
    if (studenteEsistente is null)
        return Results.NotFound();

    studenteEsistente.Nome = studenteAggiornato.Nome;
    studenteEsistente.Cognome = studenteAggiornato.Cognome;
    return Results.NoContent();
})
.WithName("UpdateStudente")
.WithOpenApi(op => new(op)
{
    Summary = "Aggiorna uno studente",
    Description = "Modifica nome e cognome di uno studente esistente."
})
.Accepts<Studente>("application/json")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.MapDelete("/api/studenti/{id}", (int id) =>
{
    var studenteDaRimuovere = studenti.FirstOrDefault(s => s.Id == id);
    if (studenteDaRimuovere is null)
        return Results.NotFound();

    studenti.Remove(studenteDaRimuovere);
    return Results.Ok();
})
.WithName("DeleteStudente")
.WithOpenApi(op => new(op)
{
    Summary = "Elimina uno studente",
    Description = "Rimuove uno studente dalla lista tramite ID."
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);


// endpoint iscrizione
app.MapPost("/api/iscrizioni", (Iscrizione nuovaIscrizione) =>
{
    var studente = studenti.FirstOrDefault(s => s.Id == nuovaIscrizione.StudenteId);
    var corso = corsi.FirstOrDefault(c => c.Id == nuovaIscrizione.CorsoId);

    if (studente is null || corso is null)
        return Results.BadRequest("Studente o corso non valido.");

    nuovaIscrizione.DataIscrizione = DateTime.Now.ToString("yyyy-MM-dd");
    iscrizioni.Add(nuovaIscrizione);
    return Results.Created("/api/iscrizioni", nuovaIscrizione);
})
.WithName("CreateIscrizione")
.WithOpenApi(op => new(op)
{
    Summary = "Crea una nuova iscrizione",
    Description = "Iscrive uno studente a un corso e assegna automaticamente la data come stringa."
})
.Accepts<Iscrizione>("application/json")
.Produces<Iscrizione>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

app.MapGet("/api/iscrizioni", () => Results.Ok(iscrizioni))
.WithName("GetIscrizioni")
.WithOpenApi(op => new(op)
{
    Summary = "Ottieni tutte le iscrizioni",
    Description = "Restituisce la lista completa delle iscrizioni studente-corso."
})
.Produces<List<Iscrizione>>(StatusCodes.Status200OK);



app.MapGet("/api/studenti/{id}/corsi", (int id) =>
{
    var corsiIscritti = iscrizioni
        .Where(i => i.StudenteId == id)
        .Select(i => corsi.FirstOrDefault(c => c.Id == i.CorsoId))
        .Where(c => c != null)
        .ToList();

    return Results.Ok(corsiIscritti);
});


app.MapGet("/api/corsi/{id}/studenti", (int id) =>
{
    var studentiIscritti = iscrizioni
        .Where(i => i.CorsoId == id)
        .Select(i => studenti.FirstOrDefault(s => s.Id == i.StudenteId))
        .Where(s => s != null)
        .ToList();

    return Results.Ok(studentiIscritti);
});



// ================== ENDPOINT CORSI ==================

app.MapGet("/api/corsi", () => Results.Ok(corsi))
   .WithName("GetCorsi")
   .WithOpenApi(op => new(op)
   {
       Summary = "Ottieni tutti i corsi",
       Description = "Restituisce la lista completa dei corsi disponibili."
   })
   .Produces<List<Corso>>(StatusCodes.Status200OK);

app.MapGet("/api/corsi/{id}", (int id) =>
{
    var corso = corsi.FirstOrDefault(c => c.Id == id);
    return corso is null ? Results.NotFound() : Results.Ok(corso);
})
.WithName("GetCorsoById")
.WithOpenApi(op => new(op)
{
    Summary = "Ottieni un singolo corso",
    Description = "Restituisce i dettagli di un corso specifico in base all'ID."
})
.Produces<Corso>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/corsi", (Corso nuovoCorso) =>
{
    nuovoCorso.Id = corsi.Count > 0 ? corsi.Max(c => c.Id) + 1 : 1;
    corsi.Add(nuovoCorso);
    return Results.Created($"/api/corsi/{nuovoCorso.Id}", nuovoCorso);
})
.WithName("CreateCorso")
.WithOpenApi(op => new(op)
{
    Summary = "Crea un nuovo corso",
    Description = "Aggiunge un nuovo corso all'elenco."
})
.Accepts<Corso>("application/json")
.Produces<Corso>(StatusCodes.Status201Created);

app.MapPut("/api/corsi/{id}", (int id, Corso corsoAggiornato) =>
{
    var corsoEsistente = corsi.FirstOrDefault(c => c.Id == id);
    if (corsoEsistente is null)
        return Results.NotFound();

    corsoEsistente.NomeCorso = corsoAggiornato.NomeCorso;
    corsoEsistente.Descrizione = corsoAggiornato.Descrizione;
    return Results.NoContent();
})
.WithName("UpdateCorso")
.WithOpenApi(op => new(op)
{
    Summary = "Aggiorna un corso",
    Description = "Modifica nome e descrizione di un corso esistente."
})
.Accepts<Corso>("application/json")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.MapDelete("/api/corsi/{id}", (int id) =>
{
    var corsoDaRimuovere = corsi.FirstOrDefault(c => c.Id == id);
    if (corsoDaRimuovere is null)
        return Results.NotFound();

    foreach (var studente in studenti)
    {
        studente.CorsiIscritti.RemoveAll(c => c.Id == id);
    }
    corsi.Remove(corsoDaRimuovere);
    return Results.Ok();
})
.WithName("DeleteCorso")
.WithOpenApi(op => new(op)
{
    Summary = "Elimina un corso",
    Description = "Rimuove un corso dall'elenco tramite ID."
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);


// ================== ENDPOINT EXTRA ==================

app.MapGet("/api/studenti/{studenteId}/corsi", (int studenteId) =>
{
    var studente = studenti.FirstOrDefault(s => s.Id == studenteId);
    if (studente is null)
        return Results.NotFound("Studente non trovato.");

    return Results.Ok(studente.CorsiIscritti);
})
.WithName("GetCorsiStudente")
.WithOpenApi(op => new(op)
{
    Summary = "Ottieni i corsi di uno studente",
    Description = "Restituisce tutti i corsi a cui è iscritto uno studente dato il suo ID."
})
.Produces<List<Corso>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.Run();
