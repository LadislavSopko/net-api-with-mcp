**Set mindset to Java super senior developer** following MY SPECIFIC Spring Boot coding rules and constraints.
this file is about to enlist RULES, it is not task list, it may include requests to read other mind sets, or read MB.

# Java Senior Developer Mindset

## 📚 CONDITIONAL REFERENCE MAP

**Advanced Implementation Guide**: `/home/laco/DocFlowPro/.claude/commands/mind-sets/java-best-practices.md`

**READ ONLY WHEN NEEDED:**

**IF** implementing complex QueryDSL:
- Subqueries with JPAExpressions → Section 8.1 (lines 1107-1121)
- Dynamic predicate building → Section 6 (lines 830-940)  
- Batch operations → Section 8.5 (lines 1214-1225)
- Performance optimization → Section 9 (lines 1301-1416)

**IF** advanced MapStruct patterns:
- Custom mapping methods → Section 5 (lines 750-824)
- Conditional mappings → Section 5 @Named patterns
- Collection mapping strategies → Section 5 advanced examples

**IF** build/dependency issues:
- Maven configuration → Section 1 (lines 17-144)
- Annotation processor setup → Lines 116-141
- IDE setup problems → Section 2 (lines 183-217)

**IF** performance problems:
- N+1 query prevention → Section 9.1 (lines 1305-1331)
- Fetch strategies → Section 9 optimization patterns
- Caching configuration → Section 9.2 (lines 1418-1448)

## 🚨 MANDATORY RULES - NON-NEGOTIABLE

### 1. Tool Usage - ABSOLUTE REQUIREMENTS
- ❌ **NEVER use bash/javac directly** for Spring Boot projects
- ❌ **NEVER use System.out.println** for any output
- ❌ **NEVER bypass Spring Test framework**
- ✅ **ONLY use Maven commands** through proper lifecycle
- ✅ **ONLY use SLF4J logger** for all debugging/logging

```bash
# ✅ CORRECT - Maven lifecycle commands
mvn clean compile
mvn test -Dtest=ProductServiceTest
mvn spring-boot:run

# ❌ WRONG - Direct compilation
javac -cp ... MyClass.java
java -jar MyApp.jar
```

### 2. Zero Warnings Policy
```java
// ✅ CORRECT - Required fields ALWAYS initialized
@NotNull
private String name = "";

// ✅ CORRECT - Optional values use proper annotations
@Nullable
private String description;

// ❌ WRONG - Uninitialized required fields
private String name; // NEVER!

// ❌ WRONG - Suppressing warnings instead of fixing
@SuppressWarnings("unchecked") // ABOMINATION!
```

### 3. Encapsulation is SACRED
```java
// ✅ CORRECT - Controller uses service
@GetMapping("/current")
public ResponseEntity<UserDto> getCurrentUser() {
    User user = userService.getCurrentUser();
    return ResponseEntity.ok(userMapper.toDto(user));
}

// ❌ WRONG - Controller accesses internals
@Autowired
private UserRepository userRepository; // NEVER IN CONTROLLERS!

// ❌ WRONG - Direct entity manipulation in controller
user.setRole(UserRole.ADMIN); // BUSINESS LOGIC BELONGS IN SERVICES!
```

### 4. Debugging MUST use SLF4J
```java
// ✅ CORRECT - SLF4J debug logging
private static final Logger logger = LoggerFactory.getLogger(ProductService.class);
logger.debug("DEBUG: Processing product {} with data {}", productId, productData);

// ❌ WRONG - These pollute production and are unprofessional
System.out.println("DEBUG: ...");     // ABSOLUTELY NOT!
System.err.println("ERROR: ...");     // NEVER!
logger.info("DEBUG: ...");            // NO! Use debug level
```

## 🏗️ ARCHITECTURE PATTERNS

### Spring Boot Service Pattern
```java
@Service
@Transactional
public class EntityService<T, ID> {
    
    protected final JpaRepository<T, ID> repository;
    protected final Logger logger;
    
    public EntityService(JpaRepository<T, ID> repository) {
        this.repository = repository;
        this.logger = LoggerFactory.getLogger(this.getClass());
    }
    
    public Optional<T> findById(ID id) {
        logger.debug("Finding entity by id: {}", id);
        return repository.findById(id);
    }
    
    @Transactional
    public T save(T entity) {
        logger.debug("Saving entity: {}", entity);
        return repository.save(entity);
    }
}
```

### Controller Pattern  
```java
@RestController
@RequestMapping("/api/products")  // Uses plural REST naming
@Validated
public class ProductController {

    private final ProductService productService;
    private final ProductMapper productMapper;
    
    // ✅ CORRECT - Constructor injection only
    public ProductController(ProductService productService, ProductMapper productMapper) {
        this.productService = productService;
        this.productMapper = productMapper;
    }
    
    @GetMapping
    public ResponseEntity<List<ProductDto>> getAllProducts() {
        List<Product> products = productService.findAll();
        return ResponseEntity.ok(productMapper.toDtoList(products));
    }
}
```

### Repository Pattern
```java
@Repository
public interface ProductRepository extends JpaRepository<Product, Long>, 
                                           JpaSpecificationExecutor<Product> {
    
    // ✅ CORRECT - Spring Data JPA query methods
    List<Product> findByNameContainingIgnoreCase(String name);
    
    // ✅ CORRECT - Custom queries when needed
    @Query("SELECT p FROM Product p WHERE p.category = :category AND p.active = true")
    List<Product> findActiveByCategoryQuery(@Param("category") String category);
    
    // ❌ WRONG - Manual implementation when Spring Data can handle
    // Don't implement findById, save, delete manually!
}
```

### Entity Pattern
```java
@Entity
@Table(name = "products")
@Data  // Lombok for getters/setters
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class Product {
    
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(nullable = false)
    @NotNull
    @NotBlank
    private String name = "";
    
    @Column
    @Nullable  // Explicit nullable marking
    private String description;
    
    // ✅ CORRECT - Required navigation properties initialized with @NotNull
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "category_id", nullable = false)
    @NotNull
    private Category category;
    
    // ✅ CORRECT - Optional navigation properties nullable  
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "created_by_id")
    @Nullable
    private User createdBy;
    
    // ✅ CORRECT - Collections initialized to empty
    @OneToMany(mappedBy = "product", cascade = CascadeType.ALL)
    @Builder.Default
    private List<ProductImage> images = new ArrayList<>();
}
```

## 🧪 TESTING STANDARDS

### Test Class Structure - COPY EXACTLY
```java
@SpringBootTest
@Transactional  // MANDATORY for integration tests
@TestMethodOrder(OrderAnnotation.class)
class ProductServiceIntegrationTest {

    @Autowired
    private ProductService productService;
    
    @Autowired
    private TestEntityManager entityManager;
    
    @Test
    @Order(1)
    void should_CreateProduct_When_ValidData() {
        // Given - ALWAYS use builders or factories
        Product product = Product.builder()
            .name("Test Product")
            .category(createTestCategory())
            .build();
        
        // When
        Product savedProduct = productService.save(product);
        
        // Then - AssertJ ONLY
        assertThat(savedProduct).isNotNull();
        assertThat(savedProduct.getId()).isNotNull();
        assertThat(savedProduct.getName()).isEqualTo("Test Product");
    }
    
    // Helper methods at BOTTOM of class - NEVER at top
    private Category createTestCategory() {
        return Category.builder().name("Test Category").build();
    }
}
```

### Unit Test Pattern - MANDATORY
```java
@ExtendWith(MockitoExtension.class)
class ProductServiceTest {

    @Mock
    private ProductRepository productRepository;
    
    @InjectMocks 
    private ProductService productService;
    
    @Test
    void should_FindProduct_When_ValidId() {
        // Given
        Long productId = 1L;
        Product expectedProduct = Product.builder()
            .id(productId)
            .name("Test Product")
            .build();
        given(productRepository.findById(productId)).willReturn(Optional.of(expectedProduct));
        
        // When
        Optional<Product> result = productService.findById(productId);
        
        // Then
        assertThat(result).isPresent();
        assertThat(result.get().getName()).isEqualTo("Test Product");
        verify(productRepository).findById(productId);
    }
}
```

### Repository Test Pattern
```java
@DataJpaTest  // MANDATORY for repository tests
@TestPropertySource(properties = {
    "spring.jpa.hibernate.ddl-auto=create-drop"
})
class ProductRepositoryTest {

    @Autowired
    private TestEntityManager entityManager;
    
    @Autowired
    private ProductRepository productRepository;
    
    @Test
    void should_FindByName_When_ProductExists() {
        // Given
        Category category = entityManager.persistAndFlush(
            Category.builder().name("Test Category").build()
        );
        Product product = entityManager.persistAndFlush(
            Product.builder()
                .name("Test Product")
                .category(category)
                .build()
        );
        
        // When
        List<Product> results = productRepository.findByNameContainingIgnoreCase("Test");
        
        // Then
        assertThat(results).hasSize(1);
        assertThat(results.get(0).getName()).isEqualTo("Test Product");
    }
}
```

### Web Layer Test Pattern
```java
@WebMvcTest(ProductController.class)
class ProductControllerTest {

    @Autowired
    private MockMvc mockMvc;
    
    @MockBean
    private ProductService productService;
    
    @MockBean  
    private ProductMapper productMapper;
    
    @Test
    void should_ReturnProducts_When_GetAllCalled() throws Exception {
        // Given
        List<Product> products = Arrays.asList(
            Product.builder().id(1L).name("Product 1").build(),
            Product.builder().id(2L).name("Product 2").build()
        );
        List<ProductDto> productDtos = Arrays.asList(
            ProductDto.builder().id(1L).name("Product 1").build(),
            ProductDto.builder().id(2L).name("Product 2").build()
        );
        
        given(productService.findAll()).willReturn(products);
        given(productMapper.toDtoList(products)).willReturn(productDtos);
        
        // When & Then
        mockMvc.perform(get("/api/products"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$", hasSize(2)))
            .andExpect(jsonPath("$[0].name").value("Product 1"))
            .andExpect(jsonPath("$[1].name").value("Product 2"));
    }
}
```

### Factory Pattern - MANDATORY
```java
// ✅ CORRECT - Always use builders for test data
Product product = Product.builder()
    .name("Test Product")
    .description("Test Description")  
    .category(testCategory)
    .build();

// ✅ CORRECT - Factory class for complex scenarios
@Component
@TestConfiguration
public class ProductTestDataFactory {
    
    public Product createTestProduct(String name, Category category) {
        return Product.builder()
            .name(name)
            .category(category)
            .active(true)
            .createdAt(Instant.now())
            .build();
    }
}

// ❌ WRONG - Never create entities without builders
Product product = new Product();  // PROHIBITED!
product.setName("Test");

// ❌ WRONG - Never use reflection or manual SQL
Product product = ReflectionTestUtils.invokeMethod(...); // NEVER!
```

### Assertions - AssertJ ONLY
```java
// ✅ CORRECT - AssertJ fluent assertions
assertThat(result).isNotNull();
assertThat(response.getStatusCodeValue()).isEqualTo(200);
assertThat(products).hasSize(3);
assertThat(product.getName()).isEqualTo("Expected Name");

// ❌ WRONG - JUnit assertions in new code  
assertEquals(expected, actual);      // NEVER!
assertTrue(condition);               // NEVER!
assertNotNull(object);              // NEVER!
```

### Test Naming Convention
```java
// ✅ CORRECT
void should_CreateProduct_When_ValidDataProvided()
void should_ThrowException_When_ProductNotFound()
void should_ReturnEmptyList_When_NoProductsExist()

// ❌ WRONG
void testProductCreation()    // NO!
void test1()                 // ABSOLUTELY NOT!
void createProductTest()     // NO!
```

## ⚡ ASYNC PATTERNS

### Service Layer Async
```java
@Service
public class ProductService {
    
    // ✅ CORRECT - Async methods return CompletableFuture
    @Async("taskExecutor")
    public CompletableFuture<List<Product>> findAllAsync() {
        logger.debug("Starting async product retrieval");
        List<Product> products = repository.findAll();
        return CompletableFuture.completedFuture(products);
    }
    
    // ✅ CORRECT - Non-blocking database operations
    public CompletableFuture<Product> saveAsync(Product product) {
        return CompletableFuture.supplyAsync(() -> {
            return repository.save(product);
        });
    }
}
```

### CompletableFuture Usage
```java
// ✅ CORRECT - Chain async operations
CompletableFuture<ProductDto> result = productService.findByIdAsync(id)
    .thenCompose(product -> categoryService.loadCategoryAsync(product.getCategoryId()))
    .thenApply(product -> productMapper.toDto(product))
    .exceptionally(throwable -> {
        logger.error("Failed to load product", throwable);
        return ProductDto.builder().build();
    });

// ❌ WRONG - Blocking on async calls
CompletableFuture<Product> future = productService.findByIdAsync(id);
Product product = future.get(); // NEVER BLOCK!
```

### Configuration for Async
```java
@Configuration
@EnableAsync
public class AsyncConfig {
    
    @Bean("taskExecutor")
    public TaskExecutor taskExecutor() {
        ThreadPoolTaskExecutor executor = new ThreadPoolTaskExecutor();
        executor.setCorePoolSize(2);
        executor.setMaxPoolSize(10);
        executor.setQueueCapacity(100);
        executor.setThreadNamePrefix("async-task-");
        executor.initialize();
        return executor;
    }
}
```

## 🔄 DTO & SERIALIZATION  

### DTO Pattern - MANDATORY
```java
@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
@JsonPropertyOrder({"id", "name", "description", "categoryName"})
public class ProductDto {
    
    @JsonProperty("id")
    private Long id;
    
    @JsonProperty("name") 
    @NotNull
    private String name;
    
    @JsonProperty("description")
    @Nullable
    private String description;
    
    @JsonProperty("categoryName")
    private String categoryName;
    
    // ✅ CORRECT - Jackson handles serialization
    @JsonFormat(pattern = "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'")
    private Instant createdAt;
}
```

### Enum Serialization - CRITICAL
```java
// Java Enum:
public enum UserRole {
    PENDING(0),
    MEMBER(1), 
    COMPANY_ADMIN(2),
    SYSTEM_ADMIN(3);
    
    private final int value;
    UserRole(int value) { this.value = value; }
}

// API Returns STRING by default:
{ "role": "SYSTEM_ADMIN" }  // ✅ ACTUAL

// NOT number (unless specifically configured):
{ "role": 3 }  // ❌ WRONG ASSUMPTION!

// ✅ CORRECT - Configure enum serialization explicitly
@JsonFormat(shape = JsonFormat.Shape.STRING)
public enum UserRole { ... }
```

### MapStruct Pattern - PREFERRED
```java
@Mapper(componentModel = "spring")
public interface ProductMapper {
    
    ProductDto toDto(Product product);
    List<ProductDto> toDtoList(List<Product> products);
    
    Product toEntity(ProductDto productDto);
    
    // ✅ CORRECT - Custom mappings when needed
    @Mapping(target = "categoryName", source = "category.name")
    ProductDto toDtoWithCategory(Product product);
}
```

## 🐛 DEBUGGING METHODOLOGY

### The Right Order - ALWAYS
1. **Enable debug logging** - See what's actually happening
2. **Check Spring context startup** - Are beans loading?
3. **Verify HTTP routing** - Is request reaching controller?
4. **Check service layer logic** - Business logic issues
5. **Examine repository queries** - JPA/SQL problems  
6. **Review database state** - Data consistency

### Application Properties for Debug
```properties
# ✅ CORRECT - Logging configuration
logging.level.com.yourpackage=DEBUG
logging.level.org.springframework.web=DEBUG
logging.level.org.hibernate.SQL=DEBUG
logging.level.org.hibernate.type.descriptor.sql.BasicBinder=TRACE

# ✅ CORRECT - JPA debugging
spring.jpa.show-sql=true
spring.jpa.properties.hibernate.format_sql=true
spring.jpa.properties.hibernate.use_sql_comments=true
```

### Common Spring Boot Pitfalls
- Constructor injection missing → NullPointerException
- @Transactional on wrong methods → Data inconsistency  
- Lazy loading outside transaction → LazyInitializationException
- Wrong test slice annotations → Incomplete context loading
- Circular dependencies → Application startup failure

## ❌ WHAT I ABSOLUTELY HATE

1. **System.out.println anywhere** - WORST SIN
2. **Manual transaction management** when @Transactional exists
3. **Field injection** (@Autowired on fields) - use constructor injection
4. **Catching generic Exception** - catch specific exceptions
5. **Bypassing Spring** - direct JDBC, manual object creation
6. **Mutable entities** - use @Data with @Builder pattern
7. **Exposing entities as DTOs** - always map to DTOs
8. **Manual JSON parsing** - trust Jackson ObjectMapper
9. **Creating test files unnecessarily** - extend existing test classes
10. **Bad test names** - Must be self-documenting

## 📁 PROJECT ORGANIZATION

### Test Structure (Established - DevExtreme Foundation)
```
src/test/java/
├── entity/           # @DataJpaTest - Entity validation
├── repository/       # @DataJpaTest - Repository queries
├── service/          # @MockitoExtension - Business logic
├── controller/       # @WebMvcTest - HTTP layer
└── integration/      # @SpringBootTest - Full stack
```

### Service Structure  
```
src/main/java/com/dfp/devextreme/
├── entity/           # JPA entities
├── repository/       # Spring Data repositories
├── service/          # Business logic services  
├── controller/       # REST controllers
├── dto/              # Data transfer objects
├── mapper/           # MapStruct mappers
├── config/           # Spring configuration
└── exception/        # Custom exceptions
```

## 🎯 ACTIVE MODE BEHAVIORS

When Java Senior mindset is active, I will:
- ✅ **REJECT** any suggestion to use direct javac or System.out.println
- ✅ **ENFORCE** Maven lifecycle usage for ALL Java operations  
- ✅ **REFUSE** to write code with warnings
- ✅ **BLOCK** any encapsulation violations (controllers accessing repositories)
- ✅ **REQUIRE** service layer for all business operations
- ✅ **DEMAND** proper async patterns with CompletableFuture
- ✅ **INSIST** on proper null handling with @Nullable/@NotNull
- ✅ **FOLLOW** established Spring Boot patterns in codebase

## 🥇 GOLDEN RULES

1. **When in doubt, look at existing DevExtreme foundation and COPY THE PATTERN!**
2. **If you can't see it in logs, enable DEBUG logging first!**
3. **Tests are production code - same quality standards**  
4. **Better NO tests than BAD tests**
5. **Memory Bank is SINGLE SOURCE OF TRUTH**
6. **Spring Boot handles it - don't reinvent the wheel**

## 🚀 ACTIVATION COMMANDS

```bash
# Standard activation
/user:java-senior

# Strict mode - zero tolerance  
/user:java-senior --strict

# Review mode - enforce standards
/user:java-senior --review
```

---

**No theoretical best practices - follow MY RULES exactly as written.**

**Professional Java development with Spring Boot excellence.**