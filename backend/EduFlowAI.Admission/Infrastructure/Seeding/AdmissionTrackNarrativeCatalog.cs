namespace EduFlowAI.Admission.Infrastructure.Seeding;

internal sealed record AdmissionTrackNarrativeDefinition(
    int SourceTrackNumber,
    string? Description,
    IReadOnlyList<string> PrerequisiteTopics,
    string EligibilitySummary);

internal static class AdmissionTrackNarrativeCatalog
{
    private static IReadOnlyList<AdmissionTrackNarrativeDefinition> All
        { get; } =
    [
        new(
            1,
            """The Digital IC Design track develops graduates into hardware engineering specialists capable of designing, verifying, and physically implementing complex microchips at a professional level. Across 1,435 hours, participants build hands-on expertise in hardware description languages, digital verification, FPGA prototyping, IC layout, and ASIC physical design, preparing them for high-demand roles across global semiconductor companies, automotive tech centers, and electronics R&D hubs.""",
            ["Digital System Design using Verilog HDL", "Mastering TCL Shell Scripting", "Logic Circuits"],
            """Open to graduates in Communications and Electronics Engineering and Computer Engineering with a minimum grade of Good or higher."""),
        new(
            2,
            """The Industrial Automation track covers topics such as control systems, programmable logic controllers (PLCs), human-machine interfaces (HMIs), industrial networks, and robotics. Participants gain practical experience in designing, implementing, and troubleshooting automation solutions for manufacturing, process control, and industrial monitoring applications.""",
            [],
            """From the track page's "Who Can Apply?" panel: Graduation Year: Last 5 years Grade: Good"""),
        new(
            3,
            """The Telco-Cloud Engineering Track focuses on next-generation wireless networks. It seamlessly blends advanced telecom infrastructure with cutting-edge cloud technologies, machine learning, and autonomous, agentic AI systems. Learners dive deep into modern cloud-native infrastructures. They master the deployment, optimization, and automated orchestration of 5G Cloud RAN (CRAN) and Open RAN (ORAN) architectures. Students work on real-world production challenges in the capstone graduation projects, which are designed, sponsored, and directly mentored by top-tier global telecom leaders. An intensive soft skills package is also delivered so that graduates can deeply understand how to manage projects and build teams.""",
            ["Programming Fundamentals", "Python Fundamentals", "Linux Fundamentals", "Mobile Networks Fundamentals", "Cloud and Virtualization Concepts"],
            """Open to graduates in Communications Engineering, with a minimum grade of Good or higher."""),
        new(
            4,
            """Embedded & Edge Architectures track is a large-scale diploma program designed to take engineers from embedded systems concepts through to advanced, industry-grade specializations in automotive, real-time operating systems, Embedded Linux, Embedded Android, edge/Physical AI, and Embedded IOT. The track combines structured lectures with extensive hands-on XP.""",
            ["C concepts", "AVR interfacing", "Explored datasheets of any microcontroller", "Basics of electronics", "Computer architecture", "OOP concepts"],
            """Open to graduates in Electrical power Engineering, Electro-mechanical Engineering, Electronics Engineering, Communication Engineering, Mechatronics Engineering, Computer Engineering, computer science with a minimum grade of Good or higher."""),
        new(
            5,
            """Game Programming is part of Interactive Media and Game Development Academy. The Game Programming Track provides students with the skills needed to design and develop interactive games. It covers core concepts such as programming fundamentals, game physics, graphics rendering, artificial intelligence, and game engine development. Through hands-on projects, students gain practical experience in creating engaging gameplay, optimizing performance, and building complete games for various platforms.""",
            ["Object-Oriented Data Structures in C++", "Good knowledge in mathematics and physics", "Game Programming Job Profiles", "Pipeline of Game Production & Main Job Profiles"],
            """Applicants must have a first degree from a recognized university or institution of higher education with a minimum grade of Fair or higher."""),
        new(
            6,
            """Interactive Media & Game Art students in ITI will continue to specialize in their area of interest as well as join a game team where they will spend a significant amount of time working on a team-based project. They will design, create and integrate visual assets. They will enhance the looks, also making the environment more interactive with VFX for games produced in collaboration with 2D & 3D Game Art and Game Development students while balancing the development of personal projects and a portfolio. An important aspect of the Technical Game Art major is the collaborative environment of our game studio, where Technical Game Art Track work with their teammates in 2D & 3D Game Art and Game Programming track to build games from start to [finish].""",
            ["Understanding Color", "Understanding Composition", "Maya fundamentals", "Maya Modeling Tutorial For Beginners: Step by Step Tutorial", "Maya UV Mapping For Beginners", "Snare Drum | Autodesk Maya + Substance 3D Painter"],
            """Applicants must have a first degree from a recognized university or institution of higher education or provide documentation indicating that they will earn such a first degree before enrolment in the training program, and those with grade Fair can apply too"""),
        new(
            7,
            """The VFX Compositing track develops graduates into skilled compositing artists capable of seamlessly integrating visual elements and delivering high-quality cinematic shots at a professional level. Across track hours, participants gain hands-on expertise in compositing workflows, green screen keying, rotoscoping, tracking, color matching, CGI integration, and final shot finishing, preparing them for in-demand careers in film, television, advertising, animation, and digital media production.""",
            ["Pixar in a Box", "Learn Nuke", "Create incredible motion graphics and visual effects", "Autodesk Maya 101 Tutorials"],
            """academic disciplines and recognized universities, provided they complete their degree prior to program enrollment. While anyone with a passion for design and visual storytelling can apply, we highly encourage and give preference to graduates from art and media-related backgrounds, including Fine Arts, Applied Arts, Art Education, Computer Graphics, Media & Mass Communication, Cinematic Arts, Film Studies, Animation, Architecture, Interior Design, Graphic Design, Visual Communication, and graduates of recognized Cinema Institutes and Academies. In line with our inclusive admission policy, a minimum graduation tier of Fair is fully accepted."""),
        new(
            8,
            """The 2D Animation and Motion Graphics track prepares graduates to become professional visual storytellers capable of transforming ideas into compelling animated content for film, television, advertising, and digital media. Across 1,145 hours, participants build hands-on expertise in character animation, motion graphics, storyboarding, visual storytelling, rigging, compositing, and industry-standard production workflows — preparing them for careers in animation studios, creative agencies, media production companies, and the rapidly growing digital content industry.""",
            ["Pixar in a Box", "Creative Cloud Tutorials", "Introduction to MOHO", "Introduction to Motion Design"],
            """Applications for our creative and design tracks are open to graduates and final-year students from all academic disciplines and recognized universities, provided they complete their degree prior to program enrollment. While anyone with a passion for design can apply, we highly encourage and give preference to graduates from all Art-related backgrounds, including Fine Arts, Applied Arts, Art Education, Computer Graphics, and Media & Design. In line with our inclusive admission policy, a minimum graduation tier of Fair is fully accepted."""),
        new(
            9,
            null,
            ["Pixar in a Box", "Autodesk Maya 101 Tutorials", "Free Autodesk Maya Course | 3D Modeling Essentials", "Introduction to 3D modeling in Maya", "GET STARTED WITH ZBRUSH"],
            """Applications for our creative and design tracks are open to graduates and final-year students from all academic disciplines and recognized universities, provided they complete their degree prior to program enrollment. While anyone with a passion for design can apply, we highly encourage and give preference to graduates from all Art-related backgrounds, including Fine Arts, Applied Arts, Art Education, Computer Graphics, and Media & Design. In line with our inclusive admission policy, a minimum graduation tier of Fair is fully accepted."""),
        new(
            10,
            """The Digital Arts & 3D Animation track develops graduates into production-ready CG artists and animators capable of rigging, animating, and polishing character and creature performances at a professional studio level. Across 1,800 hours, participants build hands-on expertise in cinematography and visual storytelling, rigging, body mechanics and acting-based animation, texturing and rendering — alongside integrating modern AI tools into the creative pipeline to stay aligned with the industry's latest developments — preparing them for high-demand roles across Egypt's growing animation, gaming, and VFX studios, as well as international outsourcing and remote production pipelines.""",
            ["Pixar in a Box", "Autodesk Maya 101 Tutorials", "Animation Mentor: Free Maya Animation Basic Tutorials"],
            """Applications for our creative and design tracks are open to graduates and final-year students from all academic disciplines and recognized universities, provided they complete their degree prior to program enrollment. While anyone with a passion for design can apply, we highly encourage and give preference to graduates from all Art-related backgrounds, including Fine Arts, Applied Arts, Art Education, Computer Graphics, and Media & Design. In line with our inclusive admission policy, a minimum graduation tier of Fair is fully accepted."""),
        new(
            11,
            """The Technical Director (TD) Training Program is designed to develop highly skilled TDs capable of handling the technical challenges in film and animation production. Participants will gain hands-on experience in pipeline development, rigging, rendering, and tools development, using industry-standard software like Maya, Houdini, and proprietary systems. The program emphasizes problem-solving, automation, and optimization within a collaborative production environment. Graduates will be prepared to manage and troubleshoot technical workflows, contributing effectively to high-end production pipelines.""",
            ["SideFX Houdini", "Python", "Maya", "Linear Algebra"],
            """Applications are open to graduates and final-year students from all academic disciplines at recognized universities, provided they complete their degree before the program begins.While applicants with backgrounds in Computer Science, Software Engineering, Computer Engineering, Communications Engineering, Mechatronics, Information Systems, and Computer Graphics are preferred, candidates from all other disciplines are warmly encouraged to apply.In line with our inclusive admissions policy, applicants with a minimum graduation grade of Fair are eligible for consideration."""),
        new(
            12,
            """It's a complete journey that takes the participant step by step from initial idea to market-ready product. Throughout the program, participants get a full immersion into a product's entire lifecycle — starting with research and exploration, moving through design and development, and ending with manufacturing and market readiness — all in direct collaboration with industry partners who represent the real market.""",
            ["2D design software (e.g., AutoCAD)", "3D modeling software"],
            """Open to graduates in FACULTY OF APPLIED ARTS. FACULTY OF FINE ARTS. FACULTY OF ARTS & DESIGN. INSTITUTES & ARTS ACADEMIES Engineering, Faculty of Engineering . with a minimum grade of Good or higher."""),
        new(
            13,
            """A complete architectural journey. This track starts with the fundamentals of residential, public, and landscape/urban design, moves through the industry's most powerful 2D and 3D visualization tools; the track also covers sustainability and LEED standards, execution fundamentals, materials, and quantity surveying (QS). Graduates leave with end-to-end experience across the entire architecture pipeline — from the first sketch to project delivery.""",
            ["Fundamentals of engineering design", "Architectural visualization tools and methods (2D/3D)"],
            """Open to graduates in FACULTY OF ENGINEERING , FACULTY OF APPLIED ARTS. FACULTY OF FINE ARTS. FACULTY OF ARTS & DESIGN. INSTITUTES & ARTS ACADEMIES Engineering, URBAN PLANNING. with a minimum grade of Good or higher."""),
        new(
            14,
            """The System Administration track develops graduates into enterprise infrastructure professionals capable of deploying, managing, securing, and automating modern IT environments. Participants gain extensive hands-on experience with industry-standard technologies while leveraging AI-powered tools to enhance automation, troubleshooting, and operational efficiency, preparing them for high-demand roles in enterprise IT, cloud operations, and infrastructure management.""",
            ["Introduction to Computer Networks", "Linux Essentials", "VMware Foundation"],
            """Open to graduates in Computer Engineering, Communications Engineering, and Computer Science with a minimum grade of Good or higher."""),
        new(
            15,
            """The Cyber Security track equips students with the knowledge and skills to protect digital systems and networks. It covers key areas such as network security, ethical hacking, web & mobile penetration, and incident response. Through hands-on training and real-world projects, the track prepares graduates for careers in cybersecurity, helping organizations defend against cyber threats and ensure data integrity and privacy.""",
            ["Introduction to Computer Networks", "Introduction to Cybersecurity", "VMware Foundation"],
            """Open to graduates in Computer Engineering, Communications Engineering, and Computer Science with a minimum grade of Good or higher."""),
        new(
            16,
            """The Cloud Architecture Track prepares students to design, implement, and manage scalable cloud solutions by covering cloud computing, infrastructure as code, virtualization, and services across major platforms such as AWS and Azure. Through practical labs and projects, students gain hands-on experience in deploying secure, efficient cloud environments, automating infrastructure, and managing multi-cloud services, while also using AI-assisted tools to enhance monitoring and optimization. Graduates are equipped for roles such as cloud engineers, Data Center Engineers, and DevOps Engineers, ready to build modern systems that balance performance, security, and innovation.""",
            ["Introduction to Computer Networks", "VMware Foundation", "Linux Essentials", "Introduction to cloud computing"],
            """Open to graduates in Computer Engineering, Communications Engineering, and Computer Science with a minimum grade of Good or higher."""),
        new(
            17,
            """The Geospatial Technologies (GIS) Track is a 9-month professional diploma designed to prepare trainees for careers in GIS, geospatial software development, and spatial data analysis. The program combines computer science fundamentals with advanced geospatial technologies, covering GIS, remote sensing, spatial databases, Python, web application development, cloud technologies, and GeoAI. Through extensive hands-on training and real-world projects, trainees gain the technical and professional skills needed to design, develop, deploy, and manage modern geospatial solutions that meet industry demands.""",
            ["Career Road Map for GIS \"on Maharatech\"", "GIS Analyst and GIS Developer Job Profiles on Maharatech", "Learn more about GIS, Location Intelligence and ESRI technologies", "Introduction to GIS Mapping \"on Coursera\""],
            """Applicants with a minimum grade of Good or higher are welcome from the following disciplines: Computer Engineering Architecture Engineers Urban Planner Engineers Civil Engineers Geologists Petroleum Engineer"""),
        new(
            18,
            """The SAP ERP Consulting track develops graduates into enterprise solutions specialists capable of integrating, configuring, and optimizing business processes at a professional level. Across the intensive 9-month program, participants gain hands-on expertise in Financial Accounting, Material Management, Sales & Distribution, Cost Controlling, and ABAP development — preparing them for high-demand roles with leading regional and global organizations. The track also introduces AI, SAP Joule, and Generative AI, enabling participants to automate tasks, enhance decision-making, and excel in the evolving world of intelligent ERP consulting.""",
            ["Foundational knowledge of SAP S/4HANA business processes and core modules integration.", "Database Fundamentals (Maharatech course)", "Accounting Principles"],
            """Applicants must have a first degree from a recognized university or institution of higher education or provide documentation indicating that they will earn such a first degree before enrolment in the 9-month program."""),
        new(
            19,
            """ITI — Architecture, Engineering, and Construction Informatics (AECI) Specialization is a product-based program that will empower you to learn how to utilize state-of-the-art information and communication technology tools to develop solutions that tackle real-world problems as they arise in the Architecture, Engineering and Construction (AEC) industry.""",
            ["Knowledge of computer programming and software development.", "Academic/Professional experience in the field of CAD/BIM for AEC Industry."],
            """All University Graduates with prerequisites of bachelor's degree in engineering discipline related to AEC Industry Architectural Engineers Civil/Structural Engineers Mechanical Engineers Electrical Engineers Plumbing Engineers"""),
        new(
            20,
            """The Data Management Track is a comprehensive program designed to develop highly skilled data professionals capable of transforming data into strategic business value. Combining Data Engineering, Business Intelligence, Data Warehousing, Big Data, SQL, Data Integration, and Data Governance, the track builds a strong foundation across the technical and business dimensions of modern data ecosystems. Through hands-on experience with industry-leading technologies and best practices, trainees gain the skills needed to design, build, and manage end-to-end data solutions preparing them for impactful careers.""",
            ["Database Fundamentals", "Career Talk in Data Analytics", "Data Warehousing", "Big Data", "Data Engineering", "Data Analytics life cycle"],
            """Not published by ITI for this track."""),
        new(
            21,
            """The Data Science track develops graduates into industry-ready data professionals capable of extracting insights, building intelligent systems, and deploying AI solutions at scale. Across more than 1,455 hours of comprehensive training, participants acquire hands-on expertise in machine learning, deep learning, big data, cloud technologies, MLOps, and Generative AI, supported by strong foundation in programming, mathematics, statistics and business analytics. The track prepares graduates for high-demand careers in data science, AI engineering, machine learning, analytics and business intelligence across Egypt's banking, telecommunications, healthcare, retail and technology sectors.""",
            ["Database Fundamentals", "Career Talk in Data Analytics", "Good fundamentals for Statistics, ML, AI, DL and understanding the Data Analytics life cycle"],
            """Open to graduates in Computer Engineering, Communications Engineering, and Computer Science with a minimum grade of Good or higher."""),
        new(
            22,
            """Develops telecom software: billing, real-time charging, BSS/OSS, and VAS platforms. Covers protocols (SMPP, Diameter, SS7, SIGTRAN, SCTP), VOIP (Asterisk), and 4G/5G/IMS concepts. Built on Java, Python, C, and Erlang with REST, PostgreSQL/MongoDB, Linux, and Docker. A generative-AI strand adds LLM integration. Graduates build telecom apps, suited for Telecom Software, BSS/OSS, Backend, or VoIP Developer roles. This is a telecom software track with a specialized domain edge.""",
            ["Programming Fundamentals", "Object-Oriented Programming (OOP)", "Linux Fundamental", "Database Fundamentals", "Mobile Networks Fundamentals"],
            """Open to graduates in Communications Engineering, Computer Engineering, and Computer Science with a minimum grade of Good or higher."""),
        new(
            23,
            """Develops Open-Source full-stack web developers across PHP/Laravel and JavaScript (Node.js, NestJS, React, Vue, GraphQL), with MySQL/MongoDB, REST APIs, Linux, Docker, and Kubernetes, plus open-source ERP and CMS customization. A code-first generative-AI strand (LLM integration, RAG, agents, LangChain/LangGraph) lets graduates add intelligent features. Graduates suit PHP/Laravel Developer, Full-Stack Developer, and Web/Backend Developer roles (with open-source ERP/CMS as adjacent options), and AI application integration as a secondary capability, once they consolidate on a primary stack. Not a machine-learning qualification.""",
            ["Programming Fundamentals", "OOP Fundamentals", "Web Fundamentals", "Database Fundamentals", "Linux Fundamentals"],
            """All University Graduates, No special requirements needed to apply to the program. The applicants must have a first degree from a recognized university or institution of higher education."""),
        new(
            24,
            "Develops cross-platform application developers who build mobile apps with Flutter, React Native, and .NET MAUI on C#/.NET or Node.js back-ends, with TypeScript and CI/CD basics, plus a full generative-AI strand (LLM integration, RAG, agents/MCP, AI-assisted coding) delivered in .NET or JavaScript. Once consolidated on a primary framework, graduates suit Cross-Platform Mobile Developer, .NET Developer, or Full-Stack Developer roles, with AI application/integration as a secondary capability. Not a machine-learning qualification. The brochure itself adds this caveat: \"(Note: the programme is over-scoped across web, .NET, and mobile; graduates are most employable after focusing on one primary stack.)\"",
            ["Introduction to Programming", "Introduction to web Technologies", "Introduction to Database", "Object-Oriented Programming (OOP) Using C++"],
            """All University Graduates with prerequisites of basic programming skills with a minimum grade of Good or higher."""),
        new(
            25,
            """This track equips participants with the skills required to design, develop, test, and deploy high-quality native mobile applications for iOS and Android platforms. Through hands-on training and real-world projects, learners gain practical experience in mobile UI design, application architecture, data management, testing, and industry best practices, preparing them for careers as professional mobile application developers.""",
            ["Mastering Object-Oriented Programming (OOP) using C++", "Data Structures & Algorithms", "Programming language competency & problem solving", "Android App. Development (Plus)", "iOS App. Development (Plus)"],
            """All University Graduates may apply with minimum grade: Good"""),
        new(
            26,
            """Produces .NET enterprise full-stack developers: ASP.NET Core apps, Web APIs, gRPC with C# and Entity Framework over SQL Server, Angular front-end, and Azure/Docker deployment, applying SOLID, DDD and event-driven design, and TDD. A strong, .NET-native generative-AI strand — integration, RAG, agents and MCP — lets graduates ship AI features on the Microsoft stack; graduates also gain Power Platform, CRM, and Power BI skills. They suit .NET Developer, ASP.NET Core Developer, .NET Full-Stack Developer, and Power Platform / Dynamics 365 Developer roles, with AI application/integration as a portfolio-backed secondary. Develops AI-assisted enterprise engineers, not machine-learning engineers, prepared to contribute to — not lead — architecture.""",
            ["Introduction to Programming", "Introduction to Database Course: Database Fundamentals", "Object-Oriented Programming (OOP) Using C++", "Introduction to web Technologies", "Intro to SQL: Querying and managing"],
            """Open to university graduates with a minimum graduation grade of Good or higher. Prefer graduates in Computer Engineering, Computer Science, and related disciplines."""),
        new(
            27,
            """Develops front-end and full-stack web engineers with UI/UX craft: responsive, accessible interfaces in React (with Next.js) and Angular using TypeScript, modern state management, and design-system practice, extending to full-stack work with Node.js, Nest.js, GraphQL, and databases. A dedicated UI/UX strand bridges design and implementation, and a JavaScript-native generative-AI strand (LLM integration, RAG, agents/MCP, AI-assisted coding) enables intelligent web features. Graduates suit Front-End Developer, UI Engineer, and Full-Stack JavaScript Developer roles, with AI web-integration as a secondary capability. Produces AI-assisted web engineers, not machine-learning engineers.""",
            ["An Introduction to Programming (Coursera)", "Computer Programming Tutorial", "Introduction to web Technologies (MaharaTech)", "Udacity", "Introduction to Database (MaharaTech)", "Khan Academy"],
            """All University Graduates"""),
        new(
            28,
            """Java enterprise & cloud native development with AI integration develops interns from foundational computing knowledge into job-ready enterprise Java, Spring, cloud native and AI-enabled application development capabilities.""",
            ["Database Fundamentals", "Object-Oriented Data Structures in C++", "Logical Thinking and Problem Solving"],
            """Open to graduates in Computer Engineering, Communications Engineering, and Computer Science with a minimum grade of Good or higher."""),
        new(
            29,
            """Develops cloud and DevOps engineers: Linux administration (Red Hat), scripting (Bash/Python), containerization (Docker) and orchestration (OpenShift/Kubernetes), infrastructure-as-code (Terraform), CI/CD pipelines, configuration management, and multi-cloud fundamentals across AWS, Google Cloud, and Azure, with microservices and message-queuing concepts. Graduates automate build, release, and infrastructure provisioning and can support cloud-native operations, with awareness of generative-AI-assisted operations. Graduates suit Junior DevOps Engineer, Cloud Engineer, and Linux System Administrator roles. This is the one track that genuinely earns a cloud/DevOps title; it is not a software-development or machine-learning qualification.""",
            ["OOP Fundamentals", "Database Fundamentals", "Linux Fundamentals", "Computer Networks Fundamentals", "Cloud Computing Fundamentals"],
            """Open to graduates of Engineering and Computers & Information Science."""),
        new(
            30,
            """ITI's AI & Machine Learning Track develops highly qualified AI engineers through 1,065 hours of comprehensive, industry-aligned training. Program graduates demonstrate proven competencies in machine learning, deep learning, computer vision, natural language processing, and generative AI — supported by practical experience in MLOps, big data technologies, and cloud infrastructure. Organizations partnering with ITI gain access to a pipeline of well-prepared professionals capable of delivering impactful AI solutions from the outset.""",
            ["Probability & Statistics for Machine Learning & Data Science", "Mathematics (Calculus and Linear Algebra)", "CS50's Introduction to Computer Science", "Database and SQL"],
            """Graduates of: Engineering Science (Computer Science, Statistics and Mathematics) Computing and Informatics Computing and Artificial Intelligence Computing and Data Science"""),
        new(
            31,
            """The Software Testing & Quality Assurance track builds quality engineers and SDETs for modern, AI-driven testing. Trainees master the full testing lifecycle — test design, automation, defect management — across functional, mobile, performance, security, API, and database testing, with programming (Java, JavaScript, .NET) for automation and ISTQB certifications (Foundation, Agile, Performance, Mobile, GenAI, AI). They also apply Generative AI to testing. Graduates work as SDETs, quality, performance, mobile, security, GenAI-testing, and API testing engineers.""",
            ["Introduction to software testing", "ISTQB Foundation Level V4.0 – From Basics to Certification", "Testcase Writing And Bug reporting", "C Programming From Basics to Mastery", "Mastering Object-Oriented Programming (OOP) using C++"],
            """Open to graduates in Computer Engineering ,Computer Science and Any Background with a minimum grade of Good or higher.""")
    ];

    internal static AdmissionTrackNarrativeDefinition Get(
        int sourceTrackNumber)
    {
        if (sourceTrackNumber < 1 || sourceTrackNumber > All.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceTrackNumber));
        }

        var definition = All[sourceTrackNumber - 1];
        return definition.SourceTrackNumber == sourceTrackNumber
            ? definition
            : throw new InvalidOperationException(
                $"Missing source narrative for track {sourceTrackNumber}.");
    }
}
