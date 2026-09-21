using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        await context.Database.MigrateAsync();

        // 1. Roles
        string[] roles = [SystemRoles.Admin, SystemRoles.Member];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }

        // 2. Admin User
        var adminEmail = "admin@giveaid.org";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "System Administrator",
                Role = SystemRoles.Admin,
                Status = UserStatuses.Active,
                PhoneNumber = "+92 51 2345678",
                Address = "Humanity Tower, Blue Area",
                City = "Islamabad",
                Profession = "Director of Operations",
                Gender = "Other",
                DateOfBirth = new DateTime(1988, 5, 15),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var res = await userManager.CreateAsync(adminUser, "A@dmin123");
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, SystemRoles.Admin);
            }
        }
        else
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(adminUser);
            await userManager.ResetPasswordAsync(adminUser, resetToken, "A@dmin123");
            if (!await userManager.IsInRoleAsync(adminUser, SystemRoles.Admin))
            {
                await userManager.AddToRoleAsync(adminUser, SystemRoles.Admin);
            }
        }

        // 3. Member User
        var memberEmail = "member@giveaid.org";
        var memberUser = await userManager.FindByEmailAsync(memberEmail);
        if (memberUser == null)
        {
            memberUser = new ApplicationUser
            {
                UserName = memberEmail,
                Email = memberEmail,
                EmailConfirmed = true,
                FullName = "Sara Khan",
                Role = SystemRoles.Member,
                Status = UserStatuses.Active,
                PhoneNumber = "+92 300 9876543",
                Address = "Gulberg III",
                City = "Lahore",
                Profession = "Community Volunteer",
                Gender = "Female",
                DateOfBirth = new DateTime(1995, 8, 20),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var res = await userManager.CreateAsync(memberUser, "Member@123456");
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(memberUser, SystemRoles.Member);
            }
        }

        // 4. Causes with real online imagery
        var causeDefinitions = new[]
        {
            new {
                Name = "Children Welfare & Nutrition",
                Slug = "children-welfare",
                Description = "Providing essential nutrition, clinical healthcare, and safe shelter for orphans and vulnerable children in high-need rural districts.",
                TargetAmount = 50000.00m,
                RaisedAmount = 34500.00m,
                ImagePath = "https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?q=80&w=900&auto=format&fit=crop"
            },
            new {
                Name = "Education For All Children",
                Slug = "education-for-all",
                Description = "Building digital classrooms, supplying school uniforms & textbooks, and funding scholarships for girls and boys in impoverished villages.",
                TargetAmount = 75000.00m,
                RaisedAmount = 51200.00m,
                ImagePath = "https://images.unsplash.com/photo-1509062522246-3755977927d7?q=80&w=900&auto=format&fit=crop"
            },
            new {
                Name = "Care for Specially Challenged",
                Slug = "specially-challenged",
                Description = "Distributing wheelchairs, prosthetic limbs, hearing aids, and operating free community physiotherapy rehabilitation centers.",
                TargetAmount = 40000.00m,
                RaisedAmount = 26800.00m,
                ImagePath = "https://images.unsplash.com/photo-1576765608535-5f04d1e3f289?q=80&w=900&auto=format&fit=crop"
            },
            new {
                Name = "Women Empowerment & Micro-Grants",
                Slug = "women-empowerment",
                Description = "Delivering vocational tailoring workshops, digital skills bootcamps, and seed capital micro-grants to female household breadwinners.",
                TargetAmount = 60000.00m,
                RaisedAmount = 44900.00m,
                ImagePath = "https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?q=80&w=900&auto=format&fit=crop"
            },
            new {
                Name = "Youth Leadership & Skill Incubation",
                Slug = "youth-skills",
                Description = "Equipping young talents with modern computer science training, career coaching, sports leagues, and enterprise mentorship.",
                TargetAmount = 35000.00m,
                RaisedAmount = 19200.00m,
                ImagePath = "https://images.unsplash.com/photo-1529156069898-49953e39b3ac?q=80&w=900&auto=format&fit=crop"
            },
            new {
                Name = "Elderly Healthcare & Social Protection",
                Slug = "elderly-care",
                Description = "Monthly door-to-door medical visits, chronic disease medication packs, and warm meals for elderly citizens without family support.",
                TargetAmount = 30000.00m,
                RaisedAmount = 21400.00m,
                ImagePath = "https://images.unsplash.com/photo-1581579438747-1dc8d17bbce4?q=80&w=900&auto=format&fit=crop"
            }
        };

        foreach (var def in causeDefinitions)
        {
            var existing = await context.Causes.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Slug == def.Slug);
            if (existing == null)
            {
                await context.Causes.AddAsync(new Cause
                {
                    Name = def.Name,
                    Slug = def.Slug,
                    Description = def.Description,
                    TargetAmount = def.TargetAmount,
                    RaisedAmount = def.RaisedAmount,
                    ImagePath = def.ImagePath,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.ImagePath = def.ImagePath;
                existing.Description = def.Description;
                existing.IsActive = true;
            }
        }
        await context.SaveChangesAsync();

        // 5. Programmes with real online imagery
        var programmeDefs = new[]
        {
            new {
                Title = "Bright Minds: Mobile Digital Literacy Bus",
                Slug = "bright-minds-digital-literacy",
                Category = "Education",
                Description = "A solar-powered tech bus travelling between rural schools offering laptops, beginner coding sessions, and digital science curriculum to young learners.",
                Location = "Northern Rural Districts",
                ImagePath = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?q=80&w=900&auto=format&fit=crop",
                DaysOffset = 5
            },
            new {
                Title = "Winter Warmth & Emergency Food Caravan 2026",
                Slug = "winter-warmth-drive-2026",
                Category = "Disaster Relief",
                Description = "Distribution of thermal winter blankets, insulated jackets, firewood heaters, and 30-day dry ration family packages across highland valleys.",
                Location = "Highland Resettlement Camps",
                ImagePath = "https://images.unsplash.com/photo-1469571486292-0ba58a3f068b?q=80&w=900&auto=format&fit=crop",
                DaysOffset = 12
            },
            new {
                Title = "Community Eye Care & Surgical Camp",
                Slug = "community-eye-care-camp",
                Category = "Healthcare",
                Description = "Free comprehensive optical examinations, prescription spectacles distribution, and 100% funded cataract corrective microsurgeries.",
                Location = "Central Community Health Complex",
                ImagePath = "https://images.unsplash.com/photo-1584515979956-d9f6e5d09982?q=80&w=900&auto=format&fit=crop",
                DaysOffset = 18
            },
            new {
                Title = "Clean Water Solar Tube-well Initiative",
                Slug = "clean-water-solar-tubewell",
                Category = "Water Sanitation",
                Description = "Drilling deep groundwater boreholes and fitting automated solar pump filtration towers to deliver bacteria-free drinking water to 500+ households.",
                Location = "Southern Arid Region",
                ImagePath = "https://images.unsplash.com/photo-1542810634-71277d95dcbb?q=80&w=900&auto=format&fit=crop",
                DaysOffset = 25
            }
        };

        foreach (var pdef in programmeDefs)
        {
            var existingProg = await context.Programmes.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Slug == pdef.Slug);
            if (existingProg == null)
            {
                await context.Programmes.AddAsync(new Programme
                {
                    Title = pdef.Title,
                    Slug = pdef.Slug,
                    Category = pdef.Category,
                    Description = pdef.Description,
                    Location = pdef.Location,
                    ImagePath = pdef.ImagePath,
                    StartAt = DateTime.UtcNow.AddDays(pdef.DaysOffset),
                    EndAt = DateTime.UtcNow.AddDays(pdef.DaysOffset + 30),
                    Status = ProgrammeStatuses.Published,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existingProg.ImagePath = pdef.ImagePath;
                existingProg.Description = pdef.Description;
                existingProg.Status = ProgrammeStatuses.Published;
            }
        }
        await context.SaveChangesAsync();

        // 6. About Sections with real online imagery
        var aboutDefs = new[]
        {
            new {
                Slug = "what-we-do",
                Title = "What We Do",
                Order = 1,
                Image = "https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?q=80&w=900&auto=format&fit=crop",
                Content = "GIVE-AID is an independent humanitarian organization operating at the grassroots level. We bridge the gap between compassionate donors and marginalized populations by delivering targeted interventions in education, healthcare, water sanitation, and disaster resilience. Our transparent on-ground execution guarantees that every contribution translates into tangible human impact."
            },
            new {
                Slug = "our-mission",
                Title = "Our Mission & Vision",
                Order = 2,
                Image = "https://images.unsplash.com/photo-1593113598332-cd288d649433?q=80&w=900&auto=format&fit=crop",
                Content = "Our mission is to eliminate structural poverty and protect human dignity across vulnerable communities through sustainable development, education, and rapid relief. We envision an equitable world where every child receives quality learning, every family enjoys healthcare access, and no one is left behind due to economic hardship."
            },
            new {
                Slug = "our-team",
                Title = "Our Leadership & Team",
                Order = 3,
                Image = "https://images.unsplash.com/photo-1582213782179-e0d53f98f2ca?q=80&w=900&auto=format&fit=crop",
                Content = "Our multidisciplinary team unites experienced aid workers, healthcare specialists, community mobilizers, and logistics experts. Driven by integrity, accountability, and empathy, our staff works side-by-side with local community leaders to ensure our projects are culturally respectful, efficient, and sustained over the long run."
            },
            new {
                Slug = "career-with-us",
                Title = "Career With Us",
                Order = 4,
                Image = "https://images.unsplash.com/photo-1521737604893-d14cc237f11d?q=80&w=900&auto=format&fit=crop",
                Content = "Are you passionate about making a lasting difference? GIVE-AID offers dynamic career opportunities in project coordination, field research, donor relations, and monitoring & evaluation. We cultivate a collaborative, mission-driven culture where innovation and compassion thrive."
            },
            new {
                Slug = "our-achievements",
                Title = "Our Achievements",
                Order = 5,
                Image = "https://images.unsplash.com/photo-1532629345422-7515f3d16bb9?q=80&w=900&auto=format&fit=crop",
                Content = "Over the past seven years, GIVE-AID has impacted over 520,000 lives across 14 provinces. We have established 48 community health clinics, renovated 65 public schools, distributed 18,000 seasonal food packs, and facilitated clean water access for 130 villages through sustainable solar filtration wells."
            },
            new {
                Slug = "our-supporters",
                Title = "Our Supporters & Benefactors",
                Order = 6,
                Image = "https://images.unsplash.com/photo-1517486808906-6ca8b3f04846?q=80&w=900&auto=format&fit=crop",
                Content = "Our work is made possible through the unwavering generosity of institutional donors, corporate CSR partners, international foundations, and thousands of dedicated monthly individual givers. Their continuous trust fuels our frontline teams every single day."
            },
            new {
                Slug = "read-about-us",
                Title = "Read About Us & Our Journey",
                Order = 7,
                Image = "https://images.unsplash.com/photo-1542810634-71277d95dcbb?q=80&w=900&auto=format&fit=crop",
                Content = "GIVE-AID began in 2017 as a volunteer collective responding to localized flood emergencies. Witnessing the severe systemic gaps in long-term rehabilitation, our founders formalized the entity into a registered NGO committed to transparency, auditable fund utilization, and community-led resilience models."
            }
        };

        foreach (var adef in aboutDefs)
        {
            var sec = await context.AboutSections.FirstOrDefaultAsync(a => a.Slug == adef.Slug);
            if (sec == null)
            {
                await context.AboutSections.AddAsync(new AboutSection
                {
                    Slug = adef.Slug,
                    Title = adef.Title,
                    SortOrder = adef.Order,
                    Content = adef.Content,
                    ImagePath = adef.Image,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                sec.ImagePath = adef.Image;
                sec.Content = adef.Content;
                sec.Title = adef.Title;
                sec.IsActive = true;
            }
        }
        await context.SaveChangesAsync();

        // 7. Seed 12+ High Quality Gallery Images
        var admin = await context.Users.FirstOrDefaultAsync(u => u.Role == SystemRoles.Admin);
        int adminId = admin?.Id ?? 1;

        if (await context.GalleryImages.CountAsync() < 6)
        {
            var galleryUrls = new[]
            {
                new { Url = "https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?q=80&w=900&auto=format&fit=crop", Title = "Children's Joy in Learning", Caption = "Young students receiving new learning kits in rural schools." },
                new { Url = "https://images.unsplash.com/photo-1593113598332-cd288d649433?q=80&w=900&auto=format&fit=crop", Title = "Emergency Food Distribution", Caption = "Volunteer teams unloading relief packages for families." },
                new { Url = "https://images.unsplash.com/photo-1509062522246-3755977927d7?q=80&w=900&auto=format&fit=crop", Title = "Classroom Renovation Drive", Caption = "Modernizing school desks and blackboard facilities." },
                new { Url = "https://images.unsplash.com/photo-1542810634-71277d95dcbb?q=80&w=900&auto=format&fit=crop", Title = "Solar Drinking Water Well", Caption = "Clean and safe tap water flowing for village families." },
                new { Url = "https://images.unsplash.com/photo-1576765608535-5f04d1e3f289?q=80&w=900&auto=format&fit=crop", Title = "Mobile Health Checkup", Caption = "Free medical consultations provided by volunteer doctors." },
                new { Url = "https://images.unsplash.com/photo-1524069290683-0457abfe42c3?q=80&w=900&auto=format&fit=crop", Title = "Digital Skills Workshop", Caption = "Empowering young students with computer literacy." },
                new { Url = "https://images.unsplash.com/photo-1469571486292-0ba58a3f068b?q=80&w=900&auto=format&fit=crop", Title = "Winter Aid Relief Mission", Caption = "Distributing warm blankets and thermal garments in the cold." },
                new { Url = "https://images.unsplash.com/photo-1582213782179-e0d53f98f2ca?q=80&w=900&auto=format&fit=crop", Title = "Community Volunteers Unity", Caption = "Passionate changemakers collaborating for humanity." },
                new { Url = "https://images.unsplash.com/photo-1532629345422-7515f3d16bb9?q=80&w=900&auto=format&fit=crop", Title = "Frontline Aid Operations", Caption = "Direct transparent allocation of resources to communities." }
            };

            foreach (var g in galleryUrls)
            {
                if (!await context.GalleryImages.AnyAsync(gi => gi.ImagePath == g.Url))
                {
                    await context.GalleryImages.AddAsync(new GalleryImage
                    {
                        ImagePath = g.Url,
                        Title = g.Title,
                        Caption = g.Caption,
                        UploadedByUserId = adminId,
                        IsActive = true,
                        SortOrder = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        // 8. Contact Info
        var contact = await context.ContactInfos.FirstOrDefaultAsync();
        if (contact == null)
        {
            await context.ContactInfos.AddAsync(new ContactInfo
            {
                Label = "GIVE-AID Main Secretariat",
                Address = "Humanity Tower, 12 Blue Area, Islamabad, Pakistan",
                Phone = "+92 (051) 234-5678",
                Email = "support@giveaid.org",
                MapUrl = "https://maps.google.com/?q=Islamabad",
                SocialLinksJson = "{\"facebook\":\"https://facebook.com/giveaid\",\"twitter\":\"https://twitter.com/giveaid\",\"instagram\":\"https://instagram.com/giveaid\",\"linkedin\":\"https://linkedin.com/company/giveaid\"}",
                IsPrimary = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }
}
