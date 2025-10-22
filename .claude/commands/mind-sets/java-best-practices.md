# Spring Boot + JPA QueryDSL Best Practices (2025)

## Table of Contents
1. [Project Setup & Dependencies](#1-project-setup--dependencies)
2. [Lombok Configuration](#2-lombok-configuration)
3. [Entity Design with Lombok](#3-entity-design-with-lombok)
4. [Repository Pattern](#4-repository-pattern)
5. [MapStruct for DTO Mapping](#5-mapstruct-for-dto-mapping)
6. [Dynamic Query Building](#6-dynamic-query-building)
7. [Service Layer Architecture](#7-service-layer-architecture)
8. [Advanced QueryDSL Features](#8-advanced-querydsl-features)
9. [Performance Optimization](#9-performance-optimization)
10. [Common Pitfalls & Solutions](#10-common-pitfalls--solutions)

---

## 1. Project Setup & Dependencies

### Maven Dependencies (Spring Boot 3.x)

```xml
<properties>
    <java.version>17</java.version>
    <spring-boot.version>3.2.0</spring-boot.version>
    <querydsl.version>5.0.0</querydsl.version>
    <mapstruct.version>1.5.5.Final</mapstruct.version>
    <lombok.version>1.18.30</lombok.version>
</properties>

<dependencies>
    <!-- Spring Boot Starters -->
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-data-jpa</artifactId>
    </dependency>
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-web</artifactId>
    </dependency>
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-validation</artifactId>
    </dependency>

    <!-- QueryDSL (Jakarta for Spring Boot 3) -->
    <dependency>
        <groupId>com.querydsl</groupId>
        <artifactId>querydsl-jpa</artifactId>
        <version>${querydsl.version}</version>
        <classifier>jakarta</classifier>
    </dependency>
    <dependency>
        <groupId>com.querydsl</groupId>
        <artifactId>querydsl-apt</artifactId>
        <version>${querydsl.version}</version>
        <classifier>jakarta</classifier>
        <scope>provided</scope>
    </dependency>

    <!-- Lombok -->
    <dependency>
        <groupId>org.projectlombok</groupId>
        <artifactId>lombok</artifactId>
        <version>${lombok.version}</version>
        <scope>provided</scope>
    </dependency>

    <!-- MapStruct -->
    <dependency>
        <groupId>org.mapstruct</groupId>
        <artifactId>mapstruct</artifactId>
        <version>${mapstruct.version}</version>
    </dependency>

    <!-- Database -->
    <dependency>
        <groupId>org.postgresql</groupId>
        <artifactId>postgresql</artifactId>
        <scope>runtime</scope>
    </dependency>
    
    <!-- For H2 development -->
    <dependency>
        <groupId>com.h2database</groupId>
        <artifactId>h2</artifactId>
        <scope>test</scope>
    </dependency>
</dependencies>
```

### Build Configuration

```xml
<build>
    <plugins>
        <plugin>
            <groupId>org.springframework.boot</groupId>
            <artifactId>spring-boot-maven-plugin</artifactId>
            <configuration>
                <excludes>
                    <exclude>
                        <groupId>org.projectlombok</groupId>
                        <artifactId>lombok</artifactId>
                    </exclude>
                </excludes>
            </configuration>
        </plugin>
        
        <plugin>
            <groupId>org.apache.maven.plugins</groupId>
            <artifactId>maven-compiler-plugin</artifactId>
            <version>3.11.0</version>
            <configuration>
                <source>17</source>
                <target>17</target>
                <annotationProcessorPaths>
                    <!-- Order matters: Lombok should be first -->
                    <path>
                        <groupId>org.projectlombok</groupId>
                        <artifactId>lombok</artifactId>
                        <version>${lombok.version}</version>
                    </path>
                    <path>
                        <groupId>org.mapstruct</groupId>
                        <artifactId>mapstruct-processor</artifactId>
                        <version>${mapstruct.version}</version>
                    </path>
                    <path>
                        <groupId>com.querydsl</groupId>
                        <artifactId>querydsl-apt</artifactId>
                        <version>${querydsl.version}</version>
                        <classifier>jakarta</classifier>
                    </path>
                    <path>
                        <groupId>jakarta.persistence</groupId>
                        <artifactId>jakarta.persistence-api</artifactId>
                        <version>3.1.0</version>
                    </path>
                </annotationProcessorPaths>
            </configuration>
        </plugin>
    </plugins>
</build>
```

### Gradle Configuration (Alternative)

```gradle
plugins {
    id 'java'
    id 'org.springframework.boot' version '3.2.0'
    id 'io.spring.dependency-management' version '1.1.4'
}

dependencies {
    implementation 'org.springframework.boot:spring-boot-starter-data-jpa'
    implementation 'org.springframework.boot:spring-boot-starter-web'
    implementation 'org.springframework.boot:spring-boot-starter-validation'
    
    // QueryDSL
    implementation 'com.querydsl:querydsl-jpa:5.0.0:jakarta'
    annotationProcessor 'com.querydsl:querydsl-apt:5.0.0:jakarta'
    annotationProcessor 'jakarta.persistence:jakarta.persistence-api'
    
    // Lombok
    compileOnly 'org.projectlombok:lombok'
    annotationProcessor 'org.projectlombok:lombok'
    
    // MapStruct
    implementation 'org.mapstruct:mapstruct:1.5.5.Final'
    annotationProcessor 'org.mapstruct:mapstruct-processor:1.5.5.Final'
    
    runtimeOnly 'org.postgresql:postgresql'
    testRuntimeOnly 'com.h2database:h2'
}
```

---

## 2. Lombok Configuration

### lombok.config (Project Root)

```properties
# This file should be in the root of your project
config.stopBubbling = true
lombok.addLombokGeneratedAnnotation = true
lombok.anyConstructor.addConstructorProperties = true

# Exclude fields from toString by default
lombok.toString.doNotUseGetters = true
lombok.equalsAndHashCode.doNotUseGetters = true

# Don't generate setters for final fields
lombok.setter.flagUsage = warning

# Log configuration
lombok.log.fieldName = log
lombok.log.fieldIsStatic = true
```

### IDE Setup

**IntelliJ IDEA:**
1. Install Lombok plugin: `File → Settings → Plugins → Search "Lombok"`
2. Enable annotation processing: `File → Settings → Build, Execution, Deployment → Compiler → Annotation Processors → Enable annotation processing`

**Eclipse/STS:**
1. Download lombok.jar from https://projectlombok.org/download
2. Run `java -jar lombok.jar`
3. Select your Eclipse installation and install

**VS Code:**
1. Install "Lombok Annotations Support for VS Code" extension
2. Ensure Java extension pack is installed

---

## 3. Entity Design with Lombok

### Base Entity

```java
package com.example.entity;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.Setter;
import lombok.experimental.SuperBuilder;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;
import org.springframework.data.annotation.CreatedDate;
import org.springframework.data.annotation.LastModifiedDate;
import org.springframework.data.jpa.domain.support.AuditingEntityListener;

import java.time.LocalDateTime;

@MappedSuperclass
@Getter
@Setter
@SuperBuilder
@NoArgsConstructor
@AllArgsConstructor
@EntityListeners(AuditingEntityListener.class)
public abstract class BaseEntity {
    
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @CreatedDate
    @Column(nullable = false, updatable = false)
    private LocalDateTime createdAt;
    
    @LastModifiedDate
    @Column(nullable = false)
    private LocalDateTime updatedAt;
    
    @Version
    private Long version;
}
```

### Product Entity

```java
package com.example.entity;

import jakarta.persistence.*;
import jakarta.validation.constraints.*;
import lombok.*;
import lombok.experimental.SuperBuilder;

import java.math.BigDecimal;
import java.util.HashSet;
import java.util.Set;

@Entity
@Table(name = "products", indexes = {
    @Index(name = "idx_product_name", columnList = "name"),
    @Index(name = "idx_product_category", columnList = "category_id")
})
@Getter
@Setter
@SuperBuilder
@NoArgsConstructor
@AllArgsConstructor
@ToString(callSuper = true, exclude = {"category", "orderItems"})
@EqualsAndHashCode(callSuper = true, onlyExplicitlyIncluded = true)
public class Product extends BaseEntity {
    
    @NotBlank(message = "Product name is required")
    @Size(min = 3, max = 100)
    @Column(nullable = false, length = 100)
    @EqualsAndHashCode.Include
    private String name;
    
    @Column(columnDefinition = "TEXT")
    private String description;
    
    @NotNull(message = "Price is required")
    @DecimalMin(value = "0.0", inclusive = false)
    @Column(nullable = false, precision = 10, scale = 2)
    private BigDecimal price;
    
    @NotBlank
    @Column(unique = true, nullable = false)
    @EqualsAndHashCode.Include
    private String sku;
    
    @Min(0)
    @Column(nullable = false)
    @Builder.Default
    private Integer stockQuantity = 0;
    
    @Enumerated(EnumType.STRING)
    @Column(length = 20)
    private ProductStatus status;
    
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "category_id")
    private Category category;
    
    @OneToMany(mappedBy = "product", cascade = CascadeType.ALL, orphanRemoval = true)
    @Builder.Default
    private Set<OrderItem> orderItems = new HashSet<>();
    
    // Helper methods
    public void addStock(int quantity) {
        this.stockQuantity += quantity;
    }
    
    public void removeStock(int quantity) {
        if (this.stockQuantity < quantity) {
            throw new IllegalArgumentException("Insufficient stock");
        }
        this.stockQuantity -= quantity;
    }
    
    public boolean isInStock() {
        return this.stockQuantity > 0;
    }
}

enum ProductStatus {
    ACTIVE, INACTIVE, DISCONTINUED
}
```

### Category Entity with Self-Reference

```java
package com.example.entity;

import jakarta.persistence.*;
import lombok.*;
import lombok.experimental.SuperBuilder;

import java.util.ArrayList;
import java.util.List;

@Entity
@Table(name = "categories")
@Getter
@Setter
@SuperBuilder
@NoArgsConstructor
@AllArgsConstructor
@ToString(callSuper = true, exclude = {"products", "children", "parent"})
@EqualsAndHashCode(callSuper = true, onlyExplicitlyIncluded = true)
public class Category extends BaseEntity {
    
    @Column(nullable = false, unique = true)
    @EqualsAndHashCode.Include
    private String name;
    
    @Column(unique = true)
    @EqualsAndHashCode.Include
    private String slug;
    
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "parent_id")
    private Category parent;
    
    @OneToMany(mappedBy = "parent", cascade = CascadeType.ALL)
    @Builder.Default
    private List<Category> children = new ArrayList<>();
    
    @OneToMany(mappedBy = "category")
    @Builder.Default
    private List<Product> products = new ArrayList<>();
}
```

---

## 4. Repository Pattern

### Base Repository

```java
package com.example.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.querydsl.QuerydslPredicateExecutor;
import org.springframework.data.repository.NoRepositoryBean;

@NoRepositoryBean
public interface BaseRepository<T, ID> extends 
    JpaRepository<T, ID>, 
    QuerydslPredicateExecutor<T> {
}
```

### Product Repository

```java
package com.example.repository;

import com.example.entity.Product;
import org.springframework.stereotype.Repository;

@Repository
public interface ProductRepository extends BaseRepository<Product, Long>, ProductRepositoryCustom {
    // Spring Data JPA methods
    Optional<Product> findBySku(String sku);
    List<Product> findByStatus(ProductStatus status);
    
    @Query("SELECT p FROM Product p WHERE p.stockQuantity < :threshold")
    List<Product> findLowStockProducts(@Param("threshold") int threshold);
}
```

### Custom Repository Interface

```java
package com.example.repository;

import com.example.dto.ProductSearchCriteria;
import com.example.dto.ProductWithCategoryDTO;
import com.example.entity.Product;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;

import java.math.BigDecimal;
import java.util.List;

public interface ProductRepositoryCustom {
    Page<Product> searchProducts(ProductSearchCriteria criteria, Pageable pageable);
    List<ProductWithCategoryDTO> findProductsWithCategory();
    List<Product> findProductsByPriceRange(BigDecimal minPrice, BigDecimal maxPrice);
    long updatePricesByCategory(Long categoryId, BigDecimal percentage);
}
```

### Custom Repository Implementation

```java
package com.example.repository.impl;

import com.example.dto.ProductSearchCriteria;
import com.example.dto.ProductWithCategoryDTO;
import com.example.entity.Product;
import com.example.entity.QCategory;
import com.example.entity.QProduct;
import com.example.repository.ProductRepositoryCustom;
import com.querydsl.core.BooleanBuilder;
import com.querydsl.core.types.Projections;
import com.querydsl.core.types.dsl.BooleanExpression;
import com.querydsl.core.types.dsl.Expressions;
import com.querydsl.jpa.impl.JPAQuery;
import com.querydsl.jpa.impl.JPAQueryFactory;
import jakarta.persistence.EntityManager;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageImpl;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Repository;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.util.StringUtils;

import java.math.BigDecimal;
import java.util.List;

@Repository
public class ProductRepositoryImpl implements ProductRepositoryCustom {
    
    private final JPAQueryFactory queryFactory;
    
    @Autowired
    public ProductRepositoryImpl(EntityManager entityManager) {
        this.queryFactory = new JPAQueryFactory(entityManager);
    }
    
    @Override
    public Page<Product> searchProducts(ProductSearchCriteria criteria, Pageable pageable) {
        QProduct product = QProduct.product;
        QCategory category = QCategory.category;
        
        JPAQuery<Product> query = queryFactory
            .selectFrom(product)
            .leftJoin(product.category, category).fetchJoin();
        
        BooleanBuilder whereClause = buildWhereClause(criteria);
        query.where(whereClause);
        
        // Apply sorting
        if (pageable.getSort().isSorted()) {
            // Handle sorting based on Pageable
            pageable.getSort().forEach(order -> {
                if (order.isAscending()) {
                    query.orderBy(Expressions.stringPath(product, order.getProperty()).asc());
                } else {
                    query.orderBy(Expressions.stringPath(product, order.getProperty()).desc());
                }
            });
        } else {
            query.orderBy(product.createdAt.desc());
        }
        
        // Get total count
        long total = query.fetchCount();
        
        // Apply pagination
        query.offset(pageable.getOffset())
            .limit(pageable.getPageSize());
        
        List<Product> content = query.fetch();
        
        return new PageImpl<>(content, pageable, total);
    }
    
    private BooleanBuilder buildWhereClause(ProductSearchCriteria criteria) {
        QProduct product = QProduct.product;
        BooleanBuilder builder = new BooleanBuilder();
        
        // Name search
        if (StringUtils.hasText(criteria.getName())) {
            builder.and(product.name.containsIgnoreCase(criteria.getName()));
        }
        
        // SKU search
        if (StringUtils.hasText(criteria.getSku())) {
            builder.and(product.sku.eq(criteria.getSku()));
        }
        
        // Price range
        if (criteria.getMinPrice() != null) {
            builder.and(product.price.goe(criteria.getMinPrice()));
        }
        if (criteria.getMaxPrice() != null) {
            builder.and(product.price.loe(criteria.getMaxPrice()));
        }
        
        // Category
        if (criteria.getCategoryId() != null) {
            builder.and(product.category.id.eq(criteria.getCategoryId()));
        }
        
        // Status
        if (criteria.getStatus() != null) {
            builder.and(product.status.eq(criteria.getStatus()));
        }
        
        // In stock only
        if (criteria.isInStockOnly()) {
            builder.and(product.stockQuantity.gt(0));
        }
        
        return builder;
    }
    
    @Override
    public List<ProductWithCategoryDTO> findProductsWithCategory() {
        QProduct product = QProduct.product;
        QCategory category = QCategory.category;
        
        return queryFactory
            .select(Projections.constructor(
                ProductWithCategoryDTO.class,
                product.id,
                product.name,
                product.price,
                category.name
            ))
            .from(product)
            .leftJoin(product.category, category)
            .where(product.status.eq(ProductStatus.ACTIVE))
            .fetch();
    }
    
    @Override
    public List<Product> findProductsByPriceRange(BigDecimal minPrice, BigDecimal maxPrice) {
        QProduct product = QProduct.product;
        
        BooleanExpression predicate = product.price.between(minPrice, maxPrice)
            .and(product.status.eq(ProductStatus.ACTIVE));
        
        return queryFactory
            .selectFrom(product)
            .where(predicate)
            .orderBy(product.price.asc())
            .fetch();
    }
    
    @Override
    @Transactional
    public long updatePricesByCategory(Long categoryId, BigDecimal percentage) {
        QProduct product = QProduct.product;
        
        return queryFactory
            .update(product)
            .set(product.price, product.price.multiply(
                BigDecimal.ONE.add(percentage.divide(BigDecimal.valueOf(100)))
            ))
            .where(product.category.id.eq(categoryId))
            .execute();
    }
}
```

### JPAQueryFactory Configuration

```java
package com.example.config;

import com.querydsl.jpa.impl.JPAQueryFactory;
import jakarta.persistence.EntityManager;
import jakarta.persistence.PersistenceContext;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class QueryDslConfig {
    
    @PersistenceContext
    private EntityManager entityManager;
    
    @Bean
    public JPAQueryFactory jpaQueryFactory() {
        return new JPAQueryFactory(entityManager);
    }
}
```

---

## 5. MapStruct for DTO Mapping

### DTOs

```java
// ProductDTO.java
package com.example.dto;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.math.BigDecimal;
import java.time.LocalDateTime;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ProductDTO {
    private Long id;
    private String name;
    private String description;
    private BigDecimal price;
    private String sku;
    private Integer stockQuantity;
    private String status;
    private String categoryName;
    private Long categoryId;
    private LocalDateTime createdAt;
    private LocalDateTime updatedAt;
}

// CreateProductRequest.java
package com.example.dto;

import jakarta.validation.constraints.*;
import lombok.Data;

import java.math.BigDecimal;

@Data
public class CreateProductRequest {
    
    @NotBlank(message = "Product name is required")
    @Size(min = 3, max = 100)
    private String name;
    
    private String description;
    
    @NotNull(message = "Price is required")
    @DecimalMin(value = "0.0", inclusive = false)
    private BigDecimal price;
    
    @NotBlank(message = "SKU is required")
    private String sku;
    
    @Min(0)
    private Integer stockQuantity = 0;
    
    private Long categoryId;
}

// UpdateProductRequest.java
package com.example.dto;

import lombok.Data;
import java.math.BigDecimal;

@Data
public class UpdateProductRequest {
    private String name;
    private String description;
    private BigDecimal price;
    private Integer stockQuantity;
    private String status;
    private Long categoryId;
}

// ProductSearchCriteria.java
package com.example.dto;

import com.example.entity.ProductStatus;
import lombok.Builder;
import lombok.Data;

import java.math.BigDecimal;

@Data
@Builder
public class ProductSearchCriteria {
    private String name;
    private String sku;
    private BigDecimal minPrice;
    private BigDecimal maxPrice;
    private Long categoryId;
    private ProductStatus status;
    private boolean inStockOnly;
}
```

### MapStruct Mapper

```java
package com.example.mapper;

import com.example.dto.*;
import com.example.entity.Category;
import com.example.entity.Product;
import com.example.entity.ProductStatus;
import org.mapstruct.*;

import java.util.List;

@Mapper(
    componentModel = "spring",
    unmappedTargetPolicy = ReportingPolicy.IGNORE,
    nullValuePropertyMappingStrategy = NullValuePropertyMappingStrategy.IGNORE,
    injectionStrategy = InjectionStrategy.CONSTRUCTOR
)
public interface ProductMapper {
    
    @Mapping(target = "categoryName", source = "category.name")
    @Mapping(target = "categoryId", source = "category.id")
    ProductDTO toDTO(Product product);
    
    List<ProductDTO> toDTOs(List<Product> products);
    
    @Mapping(target = "id", ignore = true)
    @Mapping(target = "createdAt", ignore = true)
    @Mapping(target = "updatedAt", ignore = true)
    @Mapping(target = "version", ignore = true)
    @Mapping(target = "category", source = "categoryId", qualifiedByName = "mapCategory")
    @Mapping(target = "status", constant = "ACTIVE")
    @Mapping(target = "orderItems", ignore = true)
    Product toEntity(CreateProductRequest request);
    
    @Mapping(target = "id", ignore = true)
    @Mapping(target = "createdAt", ignore = true)
    @Mapping(target = "updatedAt", ignore = true)
    @Mapping(target = "version", ignore = true)
    @Mapping(target = "category", source = "categoryId", qualifiedByName = "mapCategory")
    @Mapping(target = "status", source = "status", qualifiedByName = "mapStatus")
    @Mapping(target = "orderItems", ignore = true)
    @BeanMapping(nullValuePropertyMappingStrategy = NullValuePropertyMappingStrategy.IGNORE)
    void updateEntity(UpdateProductRequest request, @MappingTarget Product product);
    
    @Named("mapCategory")
    default Category mapCategory(Long categoryId) {
        if (categoryId == null) {
            return null;
        }
        Category category = new Category();
        category.setId(categoryId);
        return category;
    }
    
    @Named("mapStatus")
    default ProductStatus mapStatus(String status) {
        if (status == null) {
            return null;
        }
        return ProductStatus.valueOf(status.toUpperCase());
    }
    
    // Custom mapping method with additional logic
    @AfterMapping
    default void enrichProductDTO(@MappingTarget ProductDTO dto, Product product) {
        if (product.getStockQuantity() > 0) {
            dto.setStatus(dto.getStatus() + " (In Stock)");
        } else {
            dto.setStatus(dto.getStatus() + " (Out of Stock)");
        }
    }
}
```

---

## 6. Dynamic Query Building

### Advanced Query Builder Service

```java
package com.example.service;

import com.example.entity.QProduct;
import com.querydsl.core.BooleanBuilder;
import com.querydsl.core.types.Predicate;
import com.querydsl.core.types.dsl.BooleanExpression;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

@Service
public class ProductQueryBuilder {
    
    private final QProduct product = QProduct.product;
    
    public Predicate buildPredicate(ProductSearchCriteria criteria) {
        List<BooleanExpression> expressions = new ArrayList<>();
        
        // Add all non-null criteria
        Optional.ofNullable(criteria.getName())
            .filter(StringUtils::hasText)
            .map(product.name::containsIgnoreCase)
            .ifPresent(expressions::add);
        
        Optional.ofNullable(criteria.getSku())
            .filter(StringUtils::hasText)
            .map(product.sku::eq)
            .ifPresent(expressions::add);
        
        Optional.ofNullable(criteria.getMinPrice())
            .map(product.price::goe)
            .ifPresent(expressions::add);
        
        Optional.ofNullable(criteria.getMaxPrice())
            .map(product.price::loe)
            .ifPresent(expressions::add);
        
        Optional.ofNullable(criteria.getCategoryId())
            .map(product.category.id::eq)
            .ifPresent(expressions::add);
        
        Optional.ofNullable(criteria.getStatus())
            .map(product.status::eq)
            .ifPresent(expressions::add);
        
        if (criteria.isInStockOnly()) {
            expressions.add(product.stockQuantity.gt(0));
        }
        
        // Combine all expressions
        return expressions.stream()
            .reduce(BooleanExpression::and)
            .orElse(product.isNotNull());
    }
    
    // Fluent API for building complex queries
    public static class FluentQueryBuilder {
        private final BooleanBuilder builder = new BooleanBuilder();
        private final QProduct product = QProduct.product;
        
        public FluentQueryBuilder withName(String name) {
            if (StringUtils.hasText(name)) {
                builder.and(product.name.containsIgnoreCase(name));
            }
            return this;
        }
        
        public FluentQueryBuilder withPriceRange(BigDecimal min, BigDecimal max) {
            if (min != null && max != null) {
                builder.and(product.price.between(min, max));
            } else if (min != null) {
                builder.and(product.price.goe(min));
            } else if (max != null) {
                builder.and(product.price.loe(max));
            }
            return this;
        }
        
        public FluentQueryBuilder withCategory(Long categoryId) {
            if (categoryId != null) {
                builder.and(product.category.id.eq(categoryId));
            }
            return this;
        }
        
        public FluentQueryBuilder inStock() {
            builder.and(product.stockQuantity.gt(0));
            return this;
        }
        
        public FluentQueryBuilder createdAfter(LocalDateTime date) {
            if (date != null) {
                builder.and(product.createdAt.after(date));
            }
            return this;
        }
        
        public Predicate build() {
            return builder.getValue() != null ? builder : product.isNotNull();
        }
    }
}
```

---

## 7. Service Layer Architecture

### Product Service

```java
package com.example.service;

import com.example.dto.*;
import com.example.entity.Product;
import com.example.exception.ResourceNotFoundException;
import com.example.mapper.ProductMapper;
import com.example.repository.ProductRepository;
import com.querydsl.core.types.Predicate;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.cache.annotation.CacheEvict;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.util.List;

@Service
@RequiredArgsConstructor
@Slf4j
@Transactional(readOnly = true)
public class ProductService {
    
    private final ProductRepository productRepository;
    private final ProductMapper productMapper;
    private final ProductQueryBuilder queryBuilder;
    
    @Cacheable(value = "products", key = "#id")
    public ProductDTO findById(Long id) {
        log.debug("Fetching product with id: {}", id);
        Product product = productRepository.findById(id)
            .orElseThrow(() -> new ResourceNotFoundException("Product not found with id: " + id));
        return productMapper.toDTO(product);
    }
    
    public ProductDTO findBySku(String sku) {
        Product product = productRepository.findBySku(sku)
            .orElseThrow(() -> new ResourceNotFoundException("Product not found with SKU: " + sku));
        return productMapper.toDTO(product);
    }
    
    public Page<ProductDTO> searchProducts(ProductSearchCriteria criteria, Pageable pageable) {
        log.debug("Searching products with criteria: {}", criteria);
        Page<Product> products = productRepository.searchProducts(criteria, pageable);
        return products.map(productMapper::toDTO);
    }
    
    public Page<ProductDTO> findAll(Predicate predicate, Pageable pageable) {
        Page<Product> products = productRepository.findAll(predicate, pageable);
        return products.map(productMapper::toDTO);
    }
    
    @Transactional
    @CacheEvict(value = "products", allEntries = true)
    public ProductDTO createProduct(CreateProductRequest request) {
        log.info("Creating new product with SKU: {}", request.getSku());
        
        // Check if SKU already exists
        if (productRepository.findBySku(request.getSku()).isPresent()) {
            throw new IllegalArgumentException("Product with SKU already exists: " + request.getSku());
        }
        
        Product product = productMapper.toEntity(request);
        product = productRepository.save(product);
        
        log.info("Created product with id: {}", product.getId());
        return productMapper.toDTO(product);
    }
    
    @Transactional
    @CacheEvict(value = "products", key = "#id")
    public ProductDTO updateProduct(Long id, UpdateProductRequest request) {
        log.info("Updating product with id: {}", id);
        
        Product product = productRepository.findById(id)
            .orElseThrow(() -> new ResourceNotFoundException("Product not found with id: " + id));
        
        productMapper.updateEntity(request, product);
        product = productRepository.save(product);
        
        log.info("Updated product with id: {}", id);
        return productMapper.toDTO(product);
    }
    
    @Transactional
    @CacheEvict(value = "products", key = "#id")
    public void deleteProduct(Long id) {
        log.info("Deleting product with id: {}", id);
        
        if (!productRepository.existsById(id)) {
            throw new ResourceNotFoundException("Product not found with id: " + id);
        }
        
        productRepository.deleteById(id);
        log.info("Deleted product with id: {}", id);
    }
    
    @Transactional
    public void adjustStock(Long productId, int quantity, boolean isAddition) {
        Product product = productRepository.findById(productId)
            .orElseThrow(() -> new ResourceNotFoundException("Product not found with id: " + productId));
        
        if (isAddition) {
            product.addStock(quantity);
        } else {
            product.removeStock(quantity);
        }
        
        productRepository.save(product);
        log.info("Adjusted stock for product {}: {} {}", productId, 
            isAddition ? "+" : "-", quantity);
    }
    
    public List<ProductDTO> findLowStockProducts(int threshold) {
        List<Product> products = productRepository.findLowStockProducts(threshold);
        return productMapper.toDTOs(products);
    }
    
    @Transactional
    public long updatePricesByCategory(Long categoryId, BigDecimal percentageChange) {
        log.info("Updating prices for category {} by {}%", categoryId, percentageChange);
        return productRepository.updatePricesByCategory(categoryId, percentageChange);
    }
}
```

---

## 8. Advanced QueryDSL Features

### Complex Queries with Joins and Subqueries

```java
package com.example.repository.impl;

import com.example.entity.*;
import com.querydsl.core.Tuple;
import com.querydsl.core.types.dsl.*;
import com.querydsl.jpa.JPAExpressions;
import com.querydsl.jpa.impl.JPAQueryFactory;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Repository;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Repository
@RequiredArgsConstructor
public class AdvancedQueryRepository {
    
    private final JPAQueryFactory queryFactory;
    
    // 1. Subquery Example - Find products with price above category average
    public List<Product> findProductsAboveCategoryAverage() {
        QProduct product = QProduct.product;
        QProduct subProduct = new QProduct("subProduct");
        
        return queryFactory
            .selectFrom(product)
            .where(product.price.gt(
                JPAExpressions
                    .select(subProduct.price.avg())
                    .from(subProduct)
                    .where(subProduct.category.eq(product.category))
            ))
            .fetch();
    }
    
    // 2. Complex Join with Aggregation
    public List<CategoryProductStats> getCategoryStats() {
        QProduct product = QProduct.product;
        QCategory category = QCategory.category;
        
        return queryFactory
            .select(
                category.name,
                product.count(),
                product.price.avg(),
                product.price.min(),
                product.price.max(),
                product.stockQuantity.sum()
            )
            .from(category)
            .leftJoin(category.products, product)
            .groupBy(category.name)
            .having(product.count().gt(0))
            .fetch()
            .stream()
            .map(tuple -> CategoryProductStats.builder()
                .categoryName(tuple.get(category.name))
                .productCount(tuple.get(product.count()))
                .averagePrice(tuple.get(product.price.avg()))
                .minPrice(tuple.get(product.price.min()))
                .maxPrice(tuple.get(product.price.max()))
                .totalStock(tuple.get(product.stockQuantity.sum()))
                .build())
            .collect(Collectors.toList());
    }
    
    // 3. Case Expression Example
    public List<ProductPriceCategory> categorizeProductsByPrice() {
        QProduct product = QProduct.product;
        
        StringExpression priceCategory = new CaseBuilder()
            .when(product.price.lt(BigDecimal.valueOf(50)))
                .then("Budget")
            .when(product.price.between(BigDecimal.valueOf(50), BigDecimal.valueOf(200)))
                .then("Mid-Range")
            .when(product.price.gt(BigDecimal.valueOf(200)))
                .then("Premium")
            .otherwise("Unknown");
        
        return queryFactory
            .select(product.name, product.price, priceCategory)
            .from(product)
            .fetch()
            .stream()
            .map(tuple -> new ProductPriceCategory(
                tuple.get(product.name),
                tuple.get(product.price),
                tuple.get(2, String.class)
            ))
            .collect(Collectors.toList());
    }
    
    // 4. Window Functions (if supported by database)
    public List<ProductRanking> getProductRankingByCategory() {
        QProduct product = QProduct.product;
        QCategory category = QCategory.category;
        
        // Note: Window functions require native SQL or database-specific extensions
        // This is a simplified version using subqueries
        return queryFactory
            .select(
                product.name,
                product.price,
                category.name,
                JPAExpressions
                    .select(product.count())
                    .from(product)
                    .where(product.category.eq(category)
                        .and(product.price.gt(product.price)))
                    .asSubQuery()
            )
            .from(product)
            .leftJoin(product.category, category)
            .orderBy(category.name.asc(), product.price.desc())
            .fetch()
            .stream()
            .map(tuple -> new ProductRanking(
                tuple.get(product.name),
                tuple.get(product.price),
                tuple.get(category.name),
                tuple.get(3, Long.class) + 1 // Rank
            ))
            .collect(Collectors.toList());
    }
    
    // 5. Batch Operations
    @Transactional
    public long deactivateOldProducts(LocalDateTime cutoffDate) {
        QProduct product = QProduct.product;
        
        return queryFactory
            .update(product)
            .set(product.status, ProductStatus.INACTIVE)
            .where(product.createdAt.before(cutoffDate)
                .and(product.stockQuantity.eq(0)))
            .execute();
    }
    
    // 6. Complex Predicate Building
    public List<Product> findWithComplexCriteria(
        List<String> categories,
        List<String> excludedSkus,
        BigDecimal minPrice,
        Integer minStock,
        LocalDateTime createdAfter
    ) {
        QProduct product = QProduct.product;
        BooleanBuilder builder = new BooleanBuilder();
        
        // Categories IN clause
        if (categories != null && !categories.isEmpty()) {
            builder.and(product.category.name.in(categories));
        }
        
        // SKUs NOT IN clause
        if (excludedSkus != null && !excludedSkus.isEmpty()) {
            builder.and(product.sku.notIn(excludedSkus));
        }
        
        // Price and stock conditions
        if (minPrice != null) {
            builder.and(product.price.goe(minPrice));
        }
        
        if (minStock != null) {
            builder.and(product.stockQuantity.goe(minStock));
        }
        
        // Date condition
        if (createdAfter != null) {
            builder.and(product.createdAt.after(createdAfter));
        }
        
        return queryFactory
            .selectFrom(product)
            .where(builder)
            .orderBy(product.createdAt.desc())
            .fetch();
    }
}

// DTOs for complex queries
@Data
@Builder
class CategoryProductStats {
    private String categoryName;
    private Long productCount;
    private BigDecimal averagePrice;
    private BigDecimal minPrice;
    private BigDecimal maxPrice;
    private Integer totalStock;
}

@Data
@AllArgsConstructor
class ProductPriceCategory {
    private String productName;
    private BigDecimal price;
    private String category;
}

@Data
@AllArgsConstructor
class ProductRanking {
    private String productName;
    private BigDecimal price;
    private String categoryName;
    private Long rank;
}
```

---

## 9. Performance Optimization

### Fetch Strategies and N+1 Prevention

```java
package com.example.repository.impl;

import com.example.entity.*;
import com.querydsl.jpa.impl.JPAQueryFactory;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
@RequiredArgsConstructor
public class OptimizedQueryRepository {
    
    private final JPAQueryFactory queryFactory;
    
    // 1. Using Fetch Joins to prevent N+1
    public List<Product> findProductsWithCategoryOptimized() {
        QProduct product = QProduct.product;
        QCategory category = QCategory.category;
        
        return queryFactory
            .selectFrom(product)
            .leftJoin(product.category, category).fetchJoin()
            .distinct()
            .fetch();
    }
    
    // 2. Batch fetching with @BatchSize alternative
    public List<Product> findProductsWithSelectiveLoading(List<Long> productIds) {
        QProduct product = QProduct.product;
        
        // First query: Load products
        List<Product> products = queryFactory
            .selectFrom(product)
            .where(product.id.in(productIds))
            .fetch();
        
        // Second query: Load categories for all products at once
        QCategory category = QCategory.category;
        queryFactory
            .selectFrom(category)
            .where(category.id.in(
                products.stream()
                    .map(p -> p.getCategory() != null ? p.getCategory().getId() : null)
                    .filter(id -> id != null)
                    .distinct()
                    .toList()
            ))
            .fetch();
        
        return products;
    }
    
    // 3. Projection for read-only operations
    public List<ProductSummary> getProductSummaries() {
        QProduct product = QProduct.product;
        QCategory category = QCategory.category;
        
        return queryFactory
            .select(Projections.constructor(
                ProductSummary.class,
                product.id,
                product.name,
                product.price,
                product.stockQuantity,
                category.name
            ))
            .from(product)
            .leftJoin(product.category, category)
            .where(product.status.eq(ProductStatus.ACTIVE))
            .fetch();
    }
    
    // 4. Pagination with count query optimization
    public Page<Product> findProductsOptimizedPagination(
        Predicate predicate, 
        Pageable pageable
    ) {
        QProduct product = QProduct.product;
        
        // Optimized count query (without joins if possible)
        long total = queryFactory
            .select(product.id.count())
            .from(product)
            .where(predicate)
            .fetchOne();
        
        // Data query with necessary joins
        List<Product> content = queryFactory
            .selectFrom(product)
            .leftJoin(product.category).fetchJoin()
            .where(predicate)
            .offset(pageable.getOffset())
            .limit(pageable.getPageSize())
            .fetch();
        
        return new PageImpl<>(content, pageable, total);
    }
}

// DTO for projection
@Data
@AllArgsConstructor
class ProductSummary {
    private Long id;
    private String name;
    private BigDecimal price;
    private Integer stockQuantity;
    private String categoryName;
}
```

### Caching Configuration

```java
package com.example.config;

import org.springframework.cache.CacheManager;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.cache.concurrent.ConcurrentMapCacheManager;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Profile;

@Configuration
@EnableCaching
public class CacheConfig {
    
    @Bean
    @Profile("!prod")
    public CacheManager devCacheManager() {
        return new ConcurrentMapCacheManager("products", "categories");
    }
    
    // For production, use Redis or Hazelcast
    @Bean
    @Profile("prod")
    public CacheManager prodCacheManager() {
        // Configure Redis or Hazelcast cache manager
        return new ConcurrentMapCacheManager("products", "categories");
    }
}
```

---

## 10. Common Pitfalls & Solutions

### 1. Lombok with JPA Entities

**Problem:** Using `@Data` with JPA entities can cause issues with lazy loading and circular references.

**Solution:**
```java
@Entity
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
@ToString(exclude = {"category", "orderItems"}) // Exclude relationships
@EqualsAndHashCode(onlyExplicitlyIncluded = true) // Only use ID
public class Product {
    @Id
    @EqualsAndHashCode.Include
    private Long id;
    // ... other fields
}
```

### 2. QueryDSL Q-Classes Not Generated

**Problem:** Q-classes are not being generated during build.

**Solution:**
- Ensure annotation processors are in correct order (Lombok first)
- Mark `target/generated-sources/annotations` as source folder in IDE
- Clean and rebuild project

### 3. MapStruct Not Mapping Nested Objects

**Problem:** MapStruct doesn't map nested entities correctly.

**Solution:**
```java
@Mapper(componentModel = "spring", uses = {CategoryMapper.class})
public interface ProductMapper {
    // MapStruct will use CategoryMapper for category field
}
```

### 4. N+1 Query Problem

**Problem:** Multiple queries executed when accessing lazy-loaded associations.

**Solution:**
```java
// Use fetch joins
queryFactory
    .selectFrom(product)
    .leftJoin(product.category).fetchJoin()
    .fetch();
```

### 5. Transaction Management

**Problem:** Lazy initialization exceptions outside transactions.

**Solution:**
```java
@Service
@Transactional(readOnly = true) // Default read-only
public class ProductService {
    
    @Transactional // Override for write operations
    public ProductDTO createProduct(CreateProductRequest request) {
        // ...
    }
}
```

---

## Controller Example

```java
package com.example.controller;

import com.example.dto.*;
import com.example.service.ProductService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.Sort;
import org.springframework.data.web.PageableDefault;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/v1/products")
@RequiredArgsConstructor
@Tag(name = "Product Management", description = "Product CRUD operations")
public class ProductController {
    
    private final ProductService productService;
    
    @GetMapping
    @Operation(summary = "Search products")
    public ResponseEntity<Page<ProductDTO>> searchProducts(
        @ModelAttribute ProductSearchCriteria criteria,
        @PageableDefault(size = 20, sort = "createdAt", direction = Sort.Direction.DESC) Pageable pageable
    ) {
        Page<ProductDTO> products = productService.searchProducts(criteria, pageable);
        return ResponseEntity.ok(products);
    }
    
    @GetMapping("/{id}")
    @Operation(summary = "Get product by ID")
    public ResponseEntity<ProductDTO> getProduct(@PathVariable Long id) {
        ProductDTO product = productService.findById(id);
        return ResponseEntity.ok(product);
    }
    
    @GetMapping("/sku/{sku}")
    @Operation(summary = "Get product by SKU")
    public ResponseEntity<ProductDTO> getProductBySku(@PathVariable String sku) {
        ProductDTO product = productService.findBySku(sku);
        return ResponseEntity.ok(product);
    }
    
    @PostMapping
    @Operation(summary = "Create new product")
    public ResponseEntity<ProductDTO> createProduct(
        @Valid @RequestBody CreateProductRequest request
    ) {
        ProductDTO product = productService.createProduct(request);
        return ResponseEntity.status(HttpStatus.CREATED).body(product);
    }
    
    @PutMapping("/{id}")
    @Operation(summary = "Update product")
    public ResponseEntity<ProductDTO> updateProduct(
        @PathVariable Long id,
        @Valid @RequestBody UpdateProductRequest request
    ) {
        ProductDTO product = productService.updateProduct(id, request);
        return ResponseEntity.ok(product);
    }
    
    @DeleteMapping("/{id}")
    @Operation(summary = "Delete product")
    public ResponseEntity<Void> deleteProduct(@PathVariable Long id) {
        productService.deleteProduct(id);
        return ResponseEntity.noContent().build();
    }
    
    @PostMapping("/{id}/stock")
    @Operation(summary = "Adjust product stock")
    public ResponseEntity<Void> adjustStock(
        @PathVariable Long id,
        @RequestParam Integer quantity,
        @RequestParam(defaultValue = "true") boolean isAddition
    ) {
        productService.adjustStock(id, quantity, isAddition);
        return ResponseEntity.ok().build();
    }
}
```

---

## Application Properties

```yaml
# application.yml
spring:
  application:
    name: spring-querydsl-demo
  
  datasource:
    url: jdbc:postgresql://localhost:5432/product_db
    username: ${DB_USERNAME:postgres}
    password: ${DB_PASSWORD:password}
    hikari:
      maximum-pool-size: 10
      minimum-idle: 5
      connection-timeout: 30000
  
  jpa:
    hibernate:
      ddl-auto: validate
    properties:
      hibernate:
        dialect: org.hibernate.dialect.PostgreSQLDialect
        format_sql: true
        use_sql_comments: true
        default_batch_fetch_size: 16
        jdbc:
          batch_size: 25
        order_inserts: true
        order_updates: true
    show-sql: false
    open-in-view: false
  
  liquibase:
    change-log: classpath:db/changelog/db.changelog-master.xml
    enabled: true

logging:
  level:
    com.example: DEBUG
    org.hibernate.SQL: DEBUG
    org.hibernate.type.descriptor.sql.BasicBinder: TRACE
    com.querydsl.sql: DEBUG

server:
  port: 8080
  error:
    include-message: always
    include-binding-errors: always

management:
  endpoints:
    web:
      exposure:
        include: health,info,metrics,prometheus
```

---

## Testing Example

```java
package com.example.repository;

import com.example.config.QueryDslConfig;
import com.example.dto.ProductSearchCriteria;
import com.example.entity.Product;
import com.example.entity.ProductStatus;
import com.example.entity.QProduct;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.orm.jpa.DataJpaTest;
import org.springframework.context.annotation.Import;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.test.context.ActiveProfiles;

import java.math.BigDecimal;

import static org.assertj.core.api.Assertions.assertThat;

@DataJpaTest
@Import(QueryDslConfig.class)
@ActiveProfiles("test")
class ProductRepositoryTest {
    
    @Autowired
    private ProductRepository productRepository;
    
    @Test
    void shouldFindProductsByPriceRange() {
        // Given
        Product product1 = Product.builder()
            .name("Product 1")
            .sku("SKU001")
            .price(BigDecimal.valueOf(50))
            .stockQuantity(10)
            .status(ProductStatus.ACTIVE)
            .build();
        
        Product product2 = Product.builder()
            .name("Product 2")
            .sku("SKU002")
            .price(BigDecimal.valueOf(150))
            .stockQuantity(5)
            .status(ProductStatus.ACTIVE)
            .build();
        
        productRepository.save(product1);
        productRepository.save(product2);
        
        // When
        QProduct qProduct = QProduct.product;
        var predicate = qProduct.price.between(
            BigDecimal.valueOf(40), 
            BigDecimal.valueOf(100)
        );
        
        var result = productRepository.findAll(predicate);
        
        // Then
        assertThat(result).hasSize(1);
        assertThat(result.get(0).getSku()).isEqualTo("SKU001");
    }
    
    @Test
    void shouldSearchProductsWithCriteria() {
        // Given - setup test data
        
        // When
        ProductSearchCriteria criteria = ProductSearchCriteria.builder()
            .name("Product")
            .minPrice(BigDecimal.valueOf(10))
            .maxPrice(BigDecimal.valueOf(200))
            .inStockOnly(true)
            .build();
        
        Page<Product> result = productRepository.searchProducts(
            criteria, 
            PageRequest.of(0, 10)
        );
        
        // Then
        assertThat(result).isNotEmpty();
        assertThat(result.getContent()).allMatch(p -> p.getStockQuantity() > 0);
    }
}
```

---

## Conclusion

This comprehensive guide covers the modern best practices for Spring Boot + JPA QueryDSL projects in 2025:

1. **Use Lombok** to eliminate boilerplate code for getters, setters, constructors
2. **Use MapStruct** for compile-time safe DTO mapping
3. **Leverage QueryDSL** for type-safe, dynamic queries
4. **Follow repository pattern** with custom implementations for complex queries
5. **Optimize performance** with proper fetch strategies and caching
6. **Handle common pitfalls** especially with Lombok and JPA entity relationships
7. **Keep service layer clean** with proper transaction management
8. **Use proper testing** strategies for repositories and services

Remember to keep your dependencies updated and follow the evolving best practices in the Spring ecosystem!